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

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace BrickScan.WpfClient.Controls
{
    //Source code adapted from https://stackoverflow.com/questions/2006729/how-can-i-have-a-listbox-auto-scroll-when-a-new-item-is-added/2007072,
    //contribution by user 'denis morozov'.
    internal class ScrollOnCollectionChangedBehavior : Behavior<ItemsControl>
    {
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var incc = AssociatedObject.ItemsSource as INotifyCollectionChanged;

            if (incc == null)
            {
                return;
            }

            incc.CollectionChanged += OnCollectionChanged;
        }

        private void OnUnLoaded(object sender, RoutedEventArgs e)
        {
            var incc = AssociatedObject.ItemsSource as INotifyCollectionChanged;

            if (incc == null)
            {
                return;
            }

            incc.CollectionChanged -= OnCollectionChanged;
        }

        private static void BringIntoView(ItemsControl itemsControl, int index)
        {
            int count = itemsControl.Items.Count;

            if (index < count)
            {
                var item = itemsControl.Items[index];

                var frameworkElement =
                    itemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;

                if (frameworkElement == null)
                {
                    return;
                }

                frameworkElement.BringIntoView();
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                BringIntoView(AssociatedObject, AssociatedObject.Items.Count - 1);
            }

            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                BringIntoView(AssociatedObject, e.NewStartingIndex);
            }
        }

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnLoaded;
        }
    }
}
