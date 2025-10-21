const { FusesPlugin } = require('@electron-forge/plugin-fuses');
const { FuseV1Options, FuseVersion } = require('@electron/fuses');

module.exports = {
  packagerConfig: {
    asar: true,
    name: 'Astro Images',
    productName: 'Astro Images',
    executableName: 'astro-images',
    appBundleId: 'com.astro-images.app',
    appCategoryType: 'public.app-category.photography',
    icon: './icon', // Electron Forge will append the appropriate extension
    ignore: [
      /^\/src\//,
      /^\/dist\//,
      /^\/\.git/,
      /^\/\.vscode/,
      /^\/REFACTORING_STATUS\.md$/,
      /^\/FITS_TROUBLESHOOTING\.md$/,
      /^\/README\.md$/,
      /^\/\.gitignore$/,
      /^\/debug_fits_headers\.py$/,
      /^\/manual-build/,
      /^\/Test Data/
    ],
    extraResource: [
      './splash.png'
    ]
  },
  rebuildConfig: {},
  makers: [
    {
      name: '@electron-forge/maker-squirrel',
      config: {
        name: 'astro-images',
        authors: 'Ken Faubel',
        description: 'Astronomical Image Viewer - A specialized two-pane image viewer for astronomical images, particularly FITS files.',
        iconUrl: 'https://github.com/kfaubel/AstroImages/raw/main/icon.ico',
        setupIcon: './icon.ico',
        loadingGif: './splash.png'
      },
    },
    {
      name: '@electron-forge/maker-zip',
      platforms: ['darwin', 'linux'],
      config: {
        name: 'astro-images'
      }
    },
    {
      name: '@electron-forge/maker-deb',
      config: {
        name: 'astro-images',
        productName: 'Astro Images',
        genericName: 'Image Viewer',
        description: 'Astronomical Image Viewer - A specialized two-pane image viewer for astronomical images, particularly FITS files.',
        categories: ['Graphics', 'Photography', 'Science'],
        maintainer: 'Ken Faubel',
        homepage: 'https://github.com/kfaubel/AstroImages'
      },
    },
    {
      name: '@electron-forge/maker-rpm',
      config: {
        name: 'astro-images',
        productName: 'Astro Images',
        genericName: 'Image Viewer',
        description: 'Astronomical Image Viewer - A specialized two-pane image viewer for astronomical images, particularly FITS files.',
        categories: ['Graphics', 'Photography', 'Science'],
        maintainer: 'Ken Faubel',
        homepage: 'https://github.com/kfaubel/AstroImages'
      },
    },
  ],
  publishers: [
    {
      name: '@electron-forge/publisher-github',
      config: {
        repository: {
          owner: 'kfaubel',
          name: 'AstroImages'
        },
        prerelease: false,
        draft: true
      }
    }
  ],
  plugins: [
    {
      name: '@electron-forge/plugin-auto-unpack-natives',
      config: {},
    },
    // Fuses are used to enable/disable various Electron functionality
    // at package time, before code signing the application
    new FusesPlugin({
      version: FuseVersion.V1,
      [FuseV1Options.RunAsNode]: false,
      [FuseV1Options.EnableCookieEncryption]: true,
      [FuseV1Options.EnableNodeOptionsEnvironmentVariable]: false,
      [FuseV1Options.EnableNodeCliInspectArguments]: false,
      [FuseV1Options.EnableEmbeddedAsarIntegrityValidation]: true,
      [FuseV1Options.OnlyLoadAppFromAsar]: true,
    }),
  ],
};
