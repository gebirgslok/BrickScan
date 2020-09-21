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

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace BrickScan.WpfClient.Views
{
    public partial class AddPartsView
    {
        public AddPartsView()
        {
            InitializeComponent();
        }

        private static void HandleMouseEnterOrLeave(Border? imageBorder, bool isEnter)
        {
            var panel = imageBorder?.FindChildren<StackPanel>().FirstOrDefault(p => p.Name == "ImageButtonsPanel");

            if (panel != null)
            {
                panel.Visibility = isEnter ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ImageBorder_OnMouseEnter(object sender, MouseEventArgs e)
        {
            HandleMouseEnterOrLeave(sender as Border, true);
        }

        private void ImageBorder_OnMouseLeave(object sender, MouseEventArgs e)
        {
            HandleMouseEnterOrLeave(sender as Border, false);
        }
    }
}
