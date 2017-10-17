

//https://stackoverflow.com/questions/30520661/broadcasting-live-audio-through-webrtc-using-socket-io-in-node-js

var localSocket;
var mediaRecorder;
var localStream;
function startRecording() {

    navigator.mediaDevices.getUserMedia({audio: true})
        .then(function (mediaStream) {
            localStream = mediaStream;
            mediaRecorder = new MediaRecorder(mediaStream);

            mediaRecorder.onstart = function (e) {
                openConnection();
            };

            mediaRecorder.ondataavailable = function (e) {
                //send data e.data
                //this.chunks.push(e.data);
                sendBinaryMessage( e.data);
                console.log(e.data);
            };
            mediaRecorder.onstop = function (e) {
                //var blob = new Blob(this.chunks, { 'type': 'audio/ogg; codecs=opus' });
                //socket.emit('radio', blob);
                //close websocket
                closeConnection();
            };

            // Start recording
            mediaRecorder.start(250);

        }).catch((err) => {
            console.log(err);
        });

    //send local stream to server
    //Stream.write(audioStream);
}

function stopRecording() {
    mediaRecorder.stop();
    localStream.getAudioTracks()[0].stop();
}

function openConnection() {
    localSocket = new WebSocket('ws://desktop-dua4ahc:11000');
}

function closeConnection() {
    localSocket.close();
}

function sendBinaryMessage(blob) {
    if (localSocket.readyState != WebSocket.OPEN) return;
    localSocket.send(blob);
}    
