﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WebEye
{
    public sealed partial class WebCameraControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the WebCameraControl class.
        /// </summary>
        public WebCameraControl()
        {
            InitializeComponent();
        }

        private DirectShowUtilities _utilities;

        private DirectShowUtilities Utilities
        {
            get { return _utilities ?? (_utilities = new DirectShowUtilities()); }
        }

        private readonly List<WebCameraId> _captureDevices = new List<WebCameraId>();
        private void SaveVideoDevice(ref DirectShowUtilities.VideoInputDeviceInfo info)
        {
            _captureDevices.Add(new WebCameraId(info));
        }

        /// <summary>
        /// Gets a list of available video capture devices.
        /// </summary>
        /// <exception cref="Win32Exception">Failed to load the DirectShow utilities dll.</exception>
        public IEnumerable<WebCameraId> GetVideoCaptureDevices()
        {
            _captureDevices.Clear();
            Utilities.EnumVideoInputDevices(SaveVideoDevice);

            return new List<WebCameraId>(_captureDevices);
        }

        /// <summary>
        /// Initializes a capture graph.
        /// </summary>
        /// <exception cref="Win32Exception">Failed to load the DirectShow utilities dll.</exception>
        /// <exception cref="DirectShowException">Failed to setup a capture graph.</exception>
        private void InitializeCaptureGraph()
        {
            Utilities.BuildCaptureGraph();
            Utilities.AddRenderFilter(Handle);
        }

        private Boolean _isCapturing;

        /// <summary>
        /// Gets a value indicating whether the control is capturing a video stream.
        /// </summary>
        [Browsable(false)]
        public Boolean IsCapturing { get { return _isCapturing; } }

        private Boolean _captureGraphInitialized;
        private WebCameraId _currentCamera;

        /// <summary>
        /// Starts a capture.
        /// </summary>
        /// <param name="camera">The camera to capture from.</param>
        /// <exception cref="ArgumentNullException">A null reference is passed as an argument.</exception>
        /// <exception cref="Win32Exception">Failed to load the DirectShow utilities dll.</exception>
        /// <exception cref="DirectShowException">Failed to run a video capture graph.</exception>
        public void StartCapture(WebCameraId camera)
        {
            if (camera == null)
            {
                throw new ArgumentNullException();
            }

            if (!_captureGraphInitialized)
            {
                InitializeCaptureGraph();

                _captureGraphInitialized = true;
            }

            if (_isCapturing)
            {
                if (_currentCamera == camera)
                {
                    return;
                }

                StopCapture();
            }

            if (_currentCamera != null)
            {
                Utilities.ResetCaptureGraph();
                _currentCamera = null;
            }

            Utilities.AddCaptureFilter(camera.DevicePath);
            _currentCamera = camera;

            try
            {
                Utilities.Start();
                _isCapturing = true;
            }
            catch (DirectShowException)
            {
                Utilities.ResetCaptureGraph();
                _currentCamera = null;
                throw;
            }
        }

        /// <summary>
        /// Retrieves the unstretched image being captured.
        /// </summary>
        /// <returns>The current image.</returns>
        /// <exception cref="InvalidOperationException">The control is not capturing a video stream.</exception>
        /// <exception cref="DirectShowException">Failed to get the current image.</exception>
        public Bitmap GetCurrentImage()
        {
            if (!_isCapturing)
            {
                throw new InvalidOperationException();
            }

            return Utilities.GetCurrentImage();
        }

        /// <summary>
        /// Gets the unstretched video size.
        /// </summary>
        [Browsable(false)]
        public Size VideoSize
        {
            get { return _isCapturing ? Utilities.GetVideoSize() : new Size(0, 0); }
        }

        /// <summary>
        /// Stops a capture.
        /// </summary>
        /// <exception cref="InvalidOperationException">The control is not capturing a video stream.</exception>
        /// <exception cref="DirectShowException">Failed to stop a video capture graph.</exception>
        public void StopCapture()
        {
            if (!_isCapturing)
            {
                throw new InvalidOperationException();
            }

            Utilities.Stop();
            _isCapturing = false;

            Utilities.ResetCaptureGraph();
            _currentCamera = null;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (_utilities != null))
            {
                if (_isCapturing)
                {
                    StopCapture();
                }

                Utilities.DestroyCaptureGraph();
                Utilities.Dispose();
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
