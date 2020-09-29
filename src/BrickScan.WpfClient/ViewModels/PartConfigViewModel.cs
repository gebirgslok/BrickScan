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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BrickScan.Library.Core.Dto;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public class PartConfigViewModel : PropertyChangedBase
    {
        private static IEnumerable<ColorDto>? _cachedColors;

        public string? PartNo { get; set; }

        public string? AdditionalIdentifier { get; set; }

        public BitmapSource? SpecificDisplayImage { get; set; }

        [DependsOn(nameof(SpecificDisplayImage))]
        public bool CanDeleteImage => SpecificDisplayImage != null;

        public ColorDto? SelectedColor { get; set; }

        public NotifyTask<IEnumerable<ColorDto>> GetColorsNotifier { get; }

        [DependsOn(nameof(PartNo), nameof(SelectedColor))]
        public bool IsValid => !string.IsNullOrEmpty(PartNo) && SelectedColor != null;

        public PartConfigViewModel(Func<Task<IEnumerable<ColorDto>>, IEnumerable<ColorDto>, NotifyTask<IEnumerable<ColorDto>>> notifierTaskFunc, 
        HttpClient httpClient)
        {
            var task = GetColorsAsnc(httpClient);
            GetColorsNotifier = notifierTaskFunc.Invoke(task, new List<ColorDto>());
        }

        private async Task<IEnumerable<ColorDto>> GetColorsAsnc(HttpClient httpClient)
        {
            if (_cachedColors == null)
            {
                var response = await httpClient.GetAsync("dataset/colors");
                var responseString = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseString);
                _cachedColors = json["data"]?.ToObject<List<ColorDto>>() ?? new List<ColorDto>();
            }

            return _cachedColors;
        }

        public void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Filter = $"{Properties.Resources.ImageFiles} (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png"
            };

            var wasSelected = dialog.ShowDialog();

            if (wasSelected.HasValue && wasSelected.Value)
            {
                var imagePath = dialog.FileName;
                SpecificDisplayImage = new BitmapImage(new Uri(imagePath));
            }
        }

        public void DeleteImage()
        {
            SpecificDisplayImage = null;
        }
    }
}
