# Building CapyIsland for iPhone (no Mac needed)

`.github/workflows/ios-testflight.yml` builds the game on GitHub's servers and uploads it to
TestFlight. Unity exports the Xcode project on a Linux machine, then a GitHub-hosted Mac
compiles it, signs it and uploads it.

- **Bundle ID:** `com.capyisland.game` (same as Android). It's set in Player Settings → iOS.
- **Version:** taken from Player Settings (`1.0`). The **build number** is the GitHub run number,
  so each upload is automatically newer than the last.
- **Signing:** Apple's cloud-managed distribution certificate, created on demand through the
  App Store Connect API key. There are no certificate or provisioning-profile files to look after.

## One-time setup

### 1. Apple (can be done from a phone browser)

1. **Join the Apple Developer Program** at <https://developer.apple.com/programs/enroll/>
   ($99 a year). Approval can take a day or two.
2. **Register the bundle ID:** go to developer.apple.com → Account → Certificates, IDs & Profiles
   → Identifiers → **+** → App IDs → App. Choose **Explicit** and enter `com.capyisland.game`.
3. **Create the app record:** go to <https://appstoreconnect.apple.com> → Apps → **+** → New App.
   - Platform: iOS
   - Name: CapyIsland (names must be unique on the store, so use a variant if it's taken)
   - Bundle ID: `com.capyisland.game`
   - SKU: `capyisland`
4. **Create an API key:** in App Store Connect, go to Users and Access → Integrations →
   App Store Connect API → Team Keys → **+**.
   - Access: **Admin**. This is required for cloud signing.
   - Download the `.p8` file. Apple lets you download it **only once**.
   - Note the **Key ID** (next to the key) and the **Issuer ID** (above the list).
5. **Find your Team ID:** developer.apple.com → Account → Membership details.

### 2. Unity license (on the Windows PC)

1. Sign in to Unity Hub and make sure your Personal license is active.
2. Open `C:\ProgramData\Unity\Unity_lic.ulf` in Notepad and copy **everything** in it.
3. Your Unity account needs an email and a password. If you normally sign in with Google or
   Apple, set a password at id.unity.com first.

### 3. GitHub secrets

Go to the repo → Settings → Secrets and variables → Actions → **New repository secret**:

| Secret | Value |
|---|---|
| `UNITY_LICENSE` | Full contents of `Unity_lic.ulf` |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `APPLE_TEAM_ID` | Team ID (10 characters) |
| `ASC_KEY_ID` | API Key ID |
| `ASC_ISSUER_ID` | API Issuer ID |
| `ASC_KEY_P8` | Full contents of the `.p8` file, including the `-----BEGIN/END PRIVATE KEY-----` lines |

## Running a build

Go to GitHub → **Actions** → **iOS → TestFlight** → **Run workflow**. A build takes roughly
30–60 minutes. When it finishes, the build shows up in App Store Connect → TestFlight after
Apple has processed it (usually 5–30 minutes).

**To install it:** in App Store Connect → TestFlight, add yourself or friends as testers by email.
Each tester installs the **TestFlight** app on their iPhone and accepts the invite.

## Costs and limits

- GitHub Actions minutes are free up to your plan's monthly allowance, but Mac minutes count
  **10×** in private repos. Expect roughly 150–250 counted minutes per build. The free plan's
  2,000 minutes a month covers a handful of builds. Usage is at github.com → Settings → Billing.
- The first Unity run is the slowest. After that the `Library` folder is cached.

## If a run fails

- **Unity job, license errors:** re-copy `Unity_lic.ulf`. Check the email and password, and turn
  off two-factor sign-in on the Unity account if it's on.
- **Xcode job, "No Accounts" or "No profiles":** the API key needs **Admin** access, and the
  bundle ID and app record must exist (steps 1.2–1.3).
- **"Invalid binary" or SDK errors after upload:** Apple requires a recent iOS SDK. The workflow
  picks the newest stable Xcode on the `macos-26` runner.
