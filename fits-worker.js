const { Worker, isMainThread, parentPort, workerData } = require('worker_threads');
const fs = require('fs');
const { createCanvas } = require('canvas');

if (!isMainThread) {
    // This is the worker thread
    parentPort.on('message', async (data) => {
        try {
            const { filePath, applyStretch, width, height } = data;
            const result = await processFitsInWorker(filePath, applyStretch, width, height);
            parentPort.postMessage({ success: true, result });
        } catch (error) {
            parentPort.postMessage({ success: false, error: error.message });
        }
    });
}

async function processFitsInWorker(filePath, applyStretch, width, height) {
    // Move the FITS processing logic here
    // This will run in a separate thread, keeping the main thread responsive
    const buffer = fs.readFileSync(filePath);

    // Parse FITS header (you'll need to move parseFitsHeader function here too)
    // ... FITS processing logic ...

    return 'data:image/png;base64,' + canvasBuffer.toString('base64');
}

module.exports = { processFitsInWorker };