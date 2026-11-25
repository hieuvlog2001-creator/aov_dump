# Il2CppDumper Web — source port (untested)
Original core: Perfare / AndnixSH / Mr D - DS Gaming, as credited in supplied source.
Install .NET 10 SDK and Node.js, run `npx wrangler login`, then `powershell -ExecutionPolicy Bypass -File build.ps1`.
The browser runs the C# core, not the Worker. No file upload endpoint. Build and runtime have not been verified. APK/IPA extraction, Windows PE loader, script copying and original GUI settings are not ported. Extract binary and metadata first. Browser memory and Cloudflare asset limits apply.

## GitHub Actions deployment

1. Upload all files, including `.github/workflows/deploy.yml`, to a GitHub repository (branch `main`).
2. In Cloudflare, create an API token using the Edit Cloudflare Workers template, scoped to your account. Copy the account ID from the Cloudflare dashboard.
3. In GitHub repository Settings > Secrets and variables > Actions, create repository secrets `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID`.
4. Open Actions > Build and deploy Cloudflare Worker > Run workflow, or push to main.
5. Read the deployment URL in the deploy step log. The worker name is set in `wrangler.toml`.

The workflow has not been executed or the application compiled in the preparation environment. Inspect the build logs if publishing fails. No credentials are included in this archive.
