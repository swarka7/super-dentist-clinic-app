import { spawn, spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "..");
const apiProject = path.join(repositoryRoot, "src", "SuperDentist.Api");
const webProject = path.join(repositoryRoot, "src", "SuperDentist.Web");
const apiUrl = process.env.SUPERDENTIST_API_URL ?? "http://localhost:5080";
const webUrl = process.env.SUPERDENTIST_WEB_URL ?? "http://localhost:5173";
const webUri = new URL(webUrl);
const npmCommand = process.platform === "win32" ? "npm.cmd" : "npm";
const children = [];
let stopping = false;

function verifyCommand(command, shell = false) {
  const result = spawnSync(command, ["--version"], {
    cwd: repositoryRoot,
    shell,
    stdio: "ignore",
  });

  if (result.error || result.status !== 0) {
    throw new Error(`Required tool '${command}' was not found on PATH.`);
  }
}

function runChecked(command, args, options = {}) {
  const result = spawnSync(command, args, {
    cwd: repositoryRoot,
    stdio: "inherit",
    ...options,
  });

  if (result.error) {
    throw result.error;
  }

  if (result.status !== 0) {
    throw new Error(`${command} failed with exit code ${result.status}.`);
  }
}

async function waitForEndpoint(url, child, name) {
  for (let attempt = 0; attempt < 60; attempt += 1) {
    if (child.exitCode !== null) {
      throw new Error(`${name} exited before becoming ready (exit code ${child.exitCode}).`);
    }

    try {
      const response = await fetch(url, { signal: AbortSignal.timeout(2_000) });
      if (response.ok) {
        return;
      }
    } catch {
      // The service may still be binding its port.
    }

    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  throw new Error(`${name} did not become ready at ${url} within 30 seconds.`);
}

function stopChildren(exitCode = 0) {
  if (stopping) {
    return;
  }

  stopping = true;
  for (const child of children) {
    if (child.exitCode === null) {
      child.kill("SIGTERM");
    }
  }

  setTimeout(() => process.exit(exitCode), 250);
}

process.on("SIGINT", () => stopChildren(0));
process.on("SIGTERM", () => stopChildren(0));

try {
  verifyCommand("dotnet");
  verifyCommand("node");
  verifyCommand(npmCommand, process.platform === "win32");

  const viteEntryPoint = path.join(webProject, "node_modules", "vite", "bin", "vite.js");
  if (!existsSync(viteEntryPoint)) {
    throw new Error("Frontend dependencies are missing. Run 'npm ci' in src/SuperDentist.Web first.");
  }

  console.log("Building the API...");
  runChecked("dotnet", [
    "build",
    "src/SuperDentist.Api/SuperDentist.Api.csproj",
    "--nologo",
  ]);

  const apiProcess = spawn(
    "dotnet",
    [path.join("bin", "Debug", "net8.0", "SuperDentist.Api.dll"), "--urls", apiUrl],
    {
      cwd: apiProject,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: "Development",
        DOTNET_ENVIRONMENT: "Development",
      },
      stdio: "inherit",
    },
  );
  children.push(apiProcess);

  const webProcess = spawn(
    "node",
    [
      path.join("node_modules", "vite", "bin", "vite.js"),
      "--host",
      webUri.hostname,
      "--port",
      webUri.port || "5173",
      "--strictPort",
    ],
    {
      cwd: webProject,
      env: {
        ...process.env,
        VITE_API_BASE_URL: process.env.VITE_API_BASE_URL || apiUrl,
      },
      stdio: "inherit",
    },
  );
  children.push(webProcess);

  for (const [child, name] of [
    [apiProcess, "API"],
    [webProcess, "React development server"],
  ]) {
    child.once("error", (error) => {
      console.error(`${name} failed to start: ${error.message}`);
      stopChildren(1);
    });
    child.once("exit", (code) => {
      if (!stopping) {
        console.error(`${name} exited unexpectedly with code ${code}.`);
        stopChildren(code ?? 1);
      }
    });
  }

  await waitForEndpoint(`${apiUrl}/health`, apiProcess, "API");
  await waitForEndpoint(webUrl, webProcess, "React development server");

  console.log("\nSuper Dentist development services are ready:");
  console.log(`  API:     ${apiUrl}`);
  console.log(`  Swagger: ${apiUrl}/swagger`);
  console.log(`  Web:     ${webUrl}`);
  console.log("\nPress Ctrl+C to stop both services.");
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  stopChildren(1);
}
