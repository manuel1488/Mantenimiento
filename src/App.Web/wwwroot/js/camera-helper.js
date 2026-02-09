let currentStream = null;

async function startVideo(src, width, height, facingMode, dotNetHelper) {
    if(!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        console.error("getUserMedia not supported in this browser");
        if(dotNetHelper){
            dotNetHelper.invokeMethodAsync('CameraError', 'getUserMedia not supported in this browser');
        }
        return;
    }
    
    try {
        let cameraInfo = await getCameraInfo();

        if (cameraInfo.length === 0) {
            console.error("No cameras found");
            if(dotNetHelper){
                dotNetHelper.invokeMethodAsync('CameraError', 'No cameras found');
            }
            return;
        }

        const stream = await navigator.mediaDevices.getUserMedia({ 
            video: {
                width: { min: width.min, ideal: width.ideal, max: width.max },
                height: { min: height.min, ideal: height.ideal, max: height.max },
                facingMode: cameraInfo.some(c => c.facingMode === facingMode) ? facingMode : 'user'
            }
        });
        
        currentStream = stream;
        let video = document.getElementById(src);
        
        if("srcObject" in video){
            video.srcObject = stream;
        }
        else {
            video.src = window.URL.createObjectURL(stream);
        }

        video.onloadedmetadata = function(e){
            video.play();
        };

        video.style.transform = "scaleX(-1)";
        
        console.log("Video started successfully");
        
    } 
    catch (err) {
        console.error("Error accessing camera: ", err);
        if(dotNetHelper){
            dotNetHelper.invokeMethodAsync('CameraError', err.name + ": " + err.message);
        }
    }
}

function stopVideo(src){
    let video = document.getElementById(src);
    if(video && video.srcObject){
        let stream = video.srcObject;
        let tracks = stream.getTracks();
        tracks.forEach(function(track) {
            track.stop();
        });
        
        video.srcObject = null;
    }
}

function getFrame(mimeType, quality, dotNetHelper){
    if (!currentStream) {
        console.error("No hay stream disponible");
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('CameraError', 'No stream available');
        }
        return;
    }
    
    // Obtener el track de video
    let videoTrack = currentStream.getVideoTracks()[0];
    
    if (!videoTrack) {
        console.error("No hay track de video disponible");
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('CameraError', 'No video track available');
        }
        return;
    }
    
    // Usar ImageCapture API
    if ('ImageCapture' in window) {
        let imageCapture = new ImageCapture(videoTrack);
        
        // Capturar bitmap (máxima calidad)
        imageCapture.grabFrame()
            .then(imageBitmap => {
                // Convertir ImageBitmap a blob
                let canvas = document.createElement('canvas');
                canvas.width = imageBitmap.width;
                canvas.height = imageBitmap.height;
                
                let ctx = canvas.getContext('2d');
                ctx.drawImage(imageBitmap, 0, 0);
                
                canvas.toBlob(blob => {
                    let reader = new FileReader();
                    reader.onload = function() {
                        dotNetHelper.invokeMethodAsync('ProcessImage', reader.result);
                    };
                    reader.readAsDataURL(blob);
                }, mimeType, quality);
            })
            .catch(err => {
                console.error('Error capturing frame:', err);
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('CameraError', 'Error capturing frame: ' + err.message);
                }
            });
    }
    else {
        console.error("ImageCapture API not supported in this browser");
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('CameraError', 'ImageCapture API not supported');
        }
    }
}

async function getCameraInfo(dotNetHelper) {
    try {
        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoDevices = devices.filter(device => device.kind === 'videoinput');
        
        const cameraInfo = [];
        
        for (const device of videoDevices) {
            try {
                const tempStream = await navigator.mediaDevices.getUserMedia({
                    video: { deviceId: device.deviceId }
                });
                
                const track = tempStream.getVideoTracks()[0];
                const capabilities = track.getCapabilities();
                const settings = track.getSettings();
                
                track.stop();
                
                cameraInfo.push({
                    label: device.label || 'Unknown Camera',
                    deviceId: device.deviceId,
                    facingMode: capabilities.facingMode || ['unknown'],
                    maxResolution: `${capabilities.width?.max || 'N/A'}x${capabilities.height?.max || 'N/A'}`,
                    isRear: capabilities.facingMode?.includes('environment') || false,
                    isFront: capabilities.facingMode?.includes('user') || false
                });
                
            } 
            catch (err) {
                cameraInfo.push({
                    label: device.label || 'Unknown Camera',
                    deviceId: device.deviceId,
                    error: err.message
                });
            }
        }
        
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('CameraInfoResult', JSON.stringify(cameraInfo));
        }
        
        return cameraInfo;
        
    } catch (err) {
        console.error("Error obteniendo información de cámaras:", err);
        return [];
    }
}