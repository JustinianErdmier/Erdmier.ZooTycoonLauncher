/* global React */
/* eslint-disable no-undef */

// ============================================================================
// Icon set
//
//  • Small UI / feature icons  → FamFamFam "Silk" (mid-2000s, XP-era glossy
//    16px PNGs).  CC Attribution — see assets/icons-silk/LICENSE.md and the
//    credit in the About dialog.  Native size is 16px; when rendered larger
//    they are scaled with smooth interpolation.
//  • Large message / severity glyphs (32–48px) → the clean round raster GIFs
//    already in assets/ (ok / error / cancel), which stay sharp at big sizes.
//  • Launcher branding (AppMark / AppLogo / AppMainLogo) → the project's own
//    artwork, unchanged.
// ============================================================================

const Icons = {};

const SILK = "assets/icons-silk/";

// Silk PNGs are authored at 16px. At 16 we show them 1:1 (crisp); scaled up we
// use smooth interpolation, which reads far cleaner than nearest-neighbour on
// glossy antialiased artwork.
function Ico({ src, size = 16, style, alt = "" }) {
  return (
    <img
      src={src}
      width={size}
      height={size}
      alt={alt}
      draggable={false}
      style={{
        display: "inline-block",
        imageRendering: size > 16 ? "auto" : "pixelated",
        verticalAlign: "middle",
        ...style,
      }}
    />
  );
}

// Large raster GIF message icons — sharp at 24–48px.
function BigIco({ src, size = 32, style, alt = "" }) {
  return (
    <img
      src={src}
      width={size}
      height={size}
      alt={alt}
      draggable={false}
      style={{ display: "inline-block", verticalAlign: "middle", ...style }}
    />
  );
}

// ----- Branding (launcher-specific artwork) --------------------------------

Icons.AppMark = ({ size = 16 }) => (
  <img
    src="assets/ztl-title-icon.png"
    width={size}
    height={size}
    alt="Zoo Tycoon Launcher"
    style={{ imageRendering: "pixelated", verticalAlign: "middle", display: "block" }}
  />
);

Icons.AppLogo = ({ size = 64 }) => (
  <img
    src="assets/ztl-primary-icon.png"
    width={size}
    height={size}
    alt="Zoo Tycoon Launcher"
    style={{ imageRendering: "pixelated", display: "block" }}
  />
);

Icons.AppMainLogo = ({ width = 360 }) => (
  <img
    src="assets/ztl-main-logo.png"
    style={{ width, height: "auto", imageRendering: "pixelated", display: "block" }}
    alt="Zoo Tycoon Launcher"
  />
);

// ----- Large severity / message glyphs (round raster GIFs) -----------------

Icons.OkCheck    = ({ size = 32 }) => <BigIco src="assets/ok32.gif"     size={size} alt="OK" />;
Icons.MailCheck  = ({ size = 32 }) => <BigIco src="assets/ok32.gif"     size={size} alt="OK" />;
Icons.MailCross  = ({ size = 32 }) => <BigIco src="assets/error32.gif"  size={size} alt="Problem" />;

// Warning / Error / Info / Question — Silk glyphs (smooth-scaled when large).
Icons.Warning    = ({ size = 32 }) => <Ico src={SILK + "exclamation.png"}   size={size} alt="Warning" />;
Icons.Error      = ({ size = 32 }) => <BigIco src="assets/cancel32.gif"   size={size} alt="Error" />;
Icons.Info       = ({ size = 32 }) => <Ico src={SILK + "information.png"} size={size} alt="Info" />;
Icons.Question   = ({ size = 32 }) => <Ico src={SILK + "help.png"}        size={size} alt="Question" />;

// ----- Small feature / task icons (Silk) -----------------------------------

Icons.Magnifier  = ({ size = 16 }) => <Ico src={SILK + "magnifier.png"}   size={size} alt="Searching" />;
Icons.Monitor    = ({ size = 16 }) => <Ico src={SILK + "monitor.png"}     size={size} alt="Display" />;
Icons.Folder     = ({ size = 16 }) => <Ico src={SILK + "folder.png"}      size={size} alt="Folder" />;
Icons.FolderLarge= ({ size = 32 }) => <Ico src={SILK + "folder.png"}      size={size} alt="Folder" />;
Icons.Exe        = ({ size = 16 }) => <Ico src={SILK + "application.png"} size={size} alt="EXE" />;
Icons.Ini        = ({ size = 16 }) => <Ico src={SILK + "page_white_gear.png"} size={size} alt="INI" />;
Icons.Settings   = ({ size = 16 }) => <Ico src={SILK + "cog.png"}         size={size} alt="Settings" />;
Icons.HelpPage   = ({ size = 16 }) => <Ico src={SILK + "help.png"}        size={size} alt="Help" />;
Icons.MyComputer = ({ size = 32 }) => <Ico src={SILK + "computer.png"}    size={size} alt="My Computer" />;

Object.assign(window, { Icons });
