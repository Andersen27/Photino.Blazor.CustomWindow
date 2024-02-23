export async function getScreensInfo() {
    var screenDetails = await window.getScreenDetails();
    return screenDetails.screens.map(s =>
        [s.left, s.top, s.width, s.height, s.devicePixelRatio]
    );
}