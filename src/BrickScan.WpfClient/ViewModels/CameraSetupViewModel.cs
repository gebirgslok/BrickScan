#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System.Threading.Tasks;
using BrickScan.WpfClient.Model;
using BrickScan.WpfClient.Properties;
using PropertyChanged;
using Stylet;
// ReSharper disable MemberCanBePrivate.Global

namespace BrickScan.WpfClient.ViewModels
{
    public class CameraSetupViewModel : PropertyChangedBase
    {
        private readonly IVideoCapture _videoCapture;

        public UsbCamera[] AvailableUsbCameras { get; set; } = null!;

        public UsbCamera? SelectedUsbCamera { get; set; }

        [DependsOn(nameof(SelectedUsbCamera), nameof(CameraCaptureState))]
        public bool CanConnectCameraAsync => SelectedUsbCamera != null && 
                                             CameraCaptureState == CameraCaptureState.Idle || 
                                             CameraCaptureState == CameraCaptureState.Error;

        [DependsOn(nameof(CameraCaptureState))]
        public bool CanDisconnectCameraAsync => CameraCaptureState == CameraCaptureState.Capturing ||
                                                CameraCaptureState == CameraCaptureState.Error;

        public CameraCaptureState CameraCaptureState { get; private set; }

        [DependsOn(nameof(CameraCaptureState))]
        public bool IsConnected => CameraCaptureState == CameraCaptureState.Capturing;

        public int SelectedSensitivityLevel
        {
            get => Settings.Default.SelectedSensitivityLevel;
            set
            {
                if (value != Settings.Default.SelectedSensitivityLevel)
                {
                    Settings.Default.SelectedSensitivityLevel = value;
                    NotifyOfPropertyChange(nameof(SelectedSensitivityLevel));
                    Settings.Default.Save();
                }
            }
        }

        private int _selectedUsbCameraIndex;
        [DoNotNotify]
        public int SelectedUsbCameraIndex
        {
            get => _selectedUsbCameraIndex;
            set
            {
                if (value != _selectedUsbCameraIndex)
                {
                    _selectedUsbCameraIndex = value;
                    Settings.Default.SelectedCameraIndex = _selectedUsbCameraIndex;
                    Settings.Default.Save();
                }
            }
        }

        public CameraSetupViewModel(IVideoCapture videoCapture)
        {
            _videoCapture = videoCapture;
            RefreshCameraList();
            SelectedSensitivityLevel = Settings.Default.SelectedSensitivityLevel;
        }

        public async Task ConnectCameraAsync()
        {
            CameraCaptureState = CameraCaptureState.Connecting;
            await Task.Run(() =>
            {
                _videoCapture.Open(SelectedUsbCamera!.Index);
            });
            CameraCaptureState = CameraCaptureState.Capturing;
        }

        public async Task DisconnectCameraAsync()
        {
            CameraCaptureState = CameraCaptureState.Disconnecting;
            await Task.Run(() =>
            {
                _videoCapture.Close();
            });
            CameraCaptureState = CameraCaptureState.Idle;
        }

        public void RefreshCameraList()
        {
            AvailableUsbCameras = UsbCamera.FindDevices();

            if (AvailableUsbCameras.Length == 0)
            {
                SelectedUsbCamera = null;
                return;
            }

            var index = 0;
            var settingsIndex = Settings.Default.SelectedCameraIndex;

            if (settingsIndex > 0 && settingsIndex < AvailableUsbCameras.Length)
            {
                index = settingsIndex;
            }

            SelectedUsbCamera = AvailableUsbCameras[index];
        }
    }
}
