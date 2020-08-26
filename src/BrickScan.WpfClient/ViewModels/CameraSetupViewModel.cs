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
using PropertyChanged;
using Stylet;
// ReSharper disable MemberCanBePrivate.Global

namespace BrickScan.WpfClient.ViewModels
{
    public class CameraSetupViewModel : PropertyChangedBase
    {
        private readonly IUserConfiguration _userConfiguration;
        private readonly IVideoCapture _videoCapture;

        public UsbCamera[] AvailableUsbCameras { get; set; } = null!;

        public UsbCamera? SelectedUsbCamera { get; set; }

        [DependsOn(nameof(SelectedUsbCamera), nameof(CameraCaptureState))]
        public bool CanConnectCameraAsync => SelectedUsbCamera != null &&
                                             (CameraCaptureState == CameraCaptureState.Idle ||
                                              CameraCaptureState == CameraCaptureState.Error);

        [DependsOn(nameof(CameraCaptureState))]
        public bool CanDisconnectCameraAsync => CameraCaptureState == CameraCaptureState.Capturing ||
                                                CameraCaptureState == CameraCaptureState.Error;

        public CameraCaptureState CameraCaptureState { get; private set; }

        public string ConnectCameraTooltip => GetConnectCameraTooltip();

        [DependsOn(nameof(CameraCaptureState))]
        public bool IsConnected => CameraCaptureState == CameraCaptureState.Capturing;

        public int SelectedSensitivityLevel
        {
            get => _userConfiguration.SelectedSensitivityLevel;
            set
            {
                if (value != _userConfiguration.SelectedSensitivityLevel)
                {
                    _userConfiguration.SelectedSensitivityLevel = value;
                    NotifyOfPropertyChange(nameof(SelectedSensitivityLevel));
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
                    _userConfiguration.SelectedCameraIndex = _selectedUsbCameraIndex;
                }
            }
        }

        public CameraSetupViewModel(IVideoCapture videoCapture, IUserConfiguration userConfiguration)
        {
            _videoCapture = videoCapture;
            _userConfiguration = userConfiguration;
            RefreshCameraList();
            SelectedSensitivityLevel = _userConfiguration.SelectedSensitivityLevel;
        }

        private string GetConnectCameraTooltip()
        {
            if (CanConnectCameraAsync)
            {
                return Properties.Resources.ConnectCameraTooltip;
            }

            if (SelectedUsbCamera == null)
            {
                return Properties.Resources.NoUsbCameraSelectedTooltip;
            }

            return Properties.Resources.CannotConnectCameraTooltip;
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
            var settingsIndex = _userConfiguration.SelectedCameraIndex;

            if (settingsIndex > 0 && settingsIndex < AvailableUsbCameras.Length)
            {
                index = settingsIndex;
            }

            SelectedUsbCamera = AvailableUsbCameras[index];
        }
    }
}
