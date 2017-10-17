(function (window) {

    'use strict';

    function define_SaveMyPodcast() {

        var SaveMyPodcast = {};

        var localSocket;
        var mediaRecorder;
        var localStream;

        SaveMyPodcast.startRecording = function(){

            navigator.mediaDevices.getUserMedia({ audio: true })
                .then(function (mediaStream) {
                    localStream = mediaStream;
                    mediaRecorder = new MediaRecorder(mediaStream);

                    mediaRecorder.onstart = function (e) {
                        localSocket = new WebSocket('ws://localhost:11000');
                    };

                    mediaRecorder.ondataavailable = function (e) {
                        if (localSocket.readyState != WebSocket.OPEN) return;
                        localSocket.send(e.data);
                        //console.log(e.data);
                    };
                    mediaRecorder.onstop = function (e) {
                        //close websocket
                        localSocket.close();
                    };

                    // Start recording
                    // each 250 milliseconds sends a packet of audio data
                    mediaRecorder.start(250);

                }).catch((err) => {
                    console.log(err);
                });
        }

        SaveMyPodcast.stopRecording = function () {
            if (mediaRecorder !=  undefined && mediaRecorder.state !== 'inactive')
                mediaRecorder.stop();
            if (localStream != undefined)
                localStream.getAudioTracks()[0].stop();
        } 

        return SaveMyPodcast;
    }
    //define globally if it doesn't already exist
    if (typeof (SaveMyPodcast) === 'undefined') {
        window.SaveMyPodcast = define_SaveMyPodcast();
    }
    else {
        console.log("SaveMyPodcast already defined.");
    }
})(window);