/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import { Html5Qrcode } from 'html5-qrcode';

window.Html5Qrcode = Html5Qrcode;

const ScannerManager = {
  html5QrCode: null,

  init(elementId, onScanSuccess) {
    if (this.html5QrCode) {
      this.stop();
    }
    this.html5QrCode = new Html5Qrcode(elementId);
    this.onSuccess = onScanSuccess;
  },

  async start(config = { fps: 10, qrbox: { width: 250, height: 250 } }) {
    try {
      const devices = await Html5Qrcode.getCameras();
      if (devices && devices.length > 0) {
        const cameraId = devices[devices.length - 1].id; // Prefer back camera
        await this.html5QrCode.start(
          cameraId,
          config,
          (decodedText, decodedResult) => {
            this.onSuccess(decodedText, decodedResult);
            // Optional: Stop after first scan or play sound
          },
          (errorMessage) => {
            // Ignore standard scan failures (empty frames)
          },
        );
      } else {
        alert('No camera found');
      }
    } catch (err) {
      console.error('Camera start error:', err);
      alert('Error accessing camera. Check permissions.');
    }
  },

  async stop() {
    if (this.html5QrCode && this.html5QrCode.isScanning) {
      try {
        await this.html5QrCode.stop();
      } catch (err) {
        console.error('Camera stop error:', err);
      }
    }
  },
};

window.ScannerManager = ScannerManager;
