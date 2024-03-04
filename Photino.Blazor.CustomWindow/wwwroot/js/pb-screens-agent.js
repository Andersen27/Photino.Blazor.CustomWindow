export async function getScreensInfo() {

    while (true) {
        try {
            var screenDetails = await window.getScreenDetails();
            break;
        } catch { } // wait for transient activation 
    }
    
    return screenDetails.screens.map(s =>
        [s.left, s.top, s.width, s.height, s.devicePixelRatio]
    );
}