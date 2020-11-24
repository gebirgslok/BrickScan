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
using BricklinkSharp.Client;
using BrickScan.WpfClient.Inventory;

namespace BrickScan.WpfClient.Extensions
{
    internal static class PriceFixingBaseMethodExtensions
    {
        public static decimal GetFinalPrice(this PriceFixingBaseMethod p, PriceGuide priceGuide, decimal c, decimal f)
        {
            decimal baseValue;
            switch (p)
            {
                case PriceFixingBaseMethod.QuantityAvgSold:
                case PriceFixingBaseMethod.QuantityAvgStock:
                    baseValue = priceGuide.QuantityAveragePrice;
                    break;

                case PriceFixingBaseMethod.AvgSold:
                case PriceFixingBaseMethod.AvgStock:
                    baseValue = priceGuide.AveragePrice;
                    break;

                default:  
                    throw new ArgumentOutOfRangeException(nameof(p), p, null);
            }

            return (baseValue + c) * f;
        }

        public static PriceGuideType GetPriceGuideType(this PriceFixingBaseMethod p)
        {
            switch (p)
            {
                case PriceFixingBaseMethod.QuantityAvgSold:
                case PriceFixingBaseMethod.AvgSold:
                    return PriceGuideType.Sold;

                case PriceFixingBaseMethod.QuantityAvgStock:
                case PriceFixingBaseMethod.AvgStock:
                    return PriceGuideType.Stock;

                default:
                    throw new ArgumentOutOfRangeException(nameof(p), p, null);
            }
        }
    }
}