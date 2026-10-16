# Official DHARA logo integration — 16 October 2026

The exact user-supplied `DHARA LOGO.png` is now used in:

- Unity dashboard header, replacing the previous wave mark.
- Standalone application icons at the eight platform sizes (1024 down to 16 pixels).
- Web dashboard header, browser favicon and Apple touch icon.

The original PNG is copied unchanged into `Assets/Dhara/Resources/Branding/DharaLogo.png` and `Web/public/dhara-logo.png`. SHA-256 comparison also confirms the production web copy is byte-identical. Its full square artwork, dark background, lettering and colors are retained. Unity Image.preserveAspect and browser object-fit:contain prevent stretching or cropping. The UI image does not intercept clicks. The Unity sprite is imported without mipmaps or lossy texture compression.

The existing light dashboard, source/API behavior, main scene and GUID references remain intact. `DharaProject.ConfigureBranding()` applies the supplied texture to standalone icons before Windows builds. `BuildBrandedWindows()` outputs a separate `Builds/Windows-Branded/DHARA.exe`, preserving previous releases. Web local serving and production copying include the new asset.

## Actual verification

- Unity compilation and Play Mode succeeded; frame advancement was checked.
- The logo resource loaded as a Sprite. Header layout audits at 1280x800, 1600x900 and 1920x1080 each reported no text overflow or off-screen controls.
- Interactive Unity console: zero errors and warnings during inspection.
- Web logo loaded at its original 1254x1254 resolution and displayed at 80x80 on desktop / 56x56 on the small layout, with preserved proportions and no horizontal page overflow.
- The web production build passed all 27 existing data/export tests. No logo-specific unit tests were added for this cosmetic change.
- Windows branded build succeeded. All 174 packaged file hashes and extracted companion files were verified. The actual extracted player was launched; its supplied header logo and native title-bar icon were observed. The prior 275-case Unity API baseline was not rerun for this visual-only update.
- Screenshots and JSON checks are in `Verification/Screenshots/Branding` and `Verification/branding-*.json`.

Windows ZIP: `Builds/DHARA-LivingCity-Windows-x64-20261016-083620.zip` (41,765,065 bytes).

SHA-256: `E181E0E9FF8F13B71EEA457C89A3FF3ACB8972C6090F8C59874184014D28354A`.

Extract the complete ZIP and run `DHARA/DHARA.exe`. The web dashboard remains at http://127.0.0.1:5174 locally. Vercel build settings remain Root Directory `Web`, framework `Other`, build `npm run build`, output `dist`. No Vercel deployment or DNS change is performed by this branding update.
