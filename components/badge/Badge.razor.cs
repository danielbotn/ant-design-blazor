﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace AntDesign
{
    /// <summary>
    /// Small numerical value or status descriptor for UI elements.
    /// </summary>
    public partial class Badge : AntDomComponentBase
    {
        /// <summary>
        /// Customize Badge dot color
        /// </summary>
        [Parameter]
        public string Color { get; set; }

        [Parameter]
        public PresetColor? PresetColor { get; set; }

        /// <summary>
        /// Number to show in badge
        /// </summary>
        [Parameter]
        public int? Count
        {
            get => _count;
            set
            {
                if (_count == value)
                {
                    return;
                }

                _count = value;

                if (_count > 0)
                {
                    this._countArray = DigitsFromInteger(_count.Value);
                }
            }
        }

        [Parameter]
        public RenderFragment CountTemplate { get; set; }

        /// <summary>
        /// Whether to display a red dot instead of count
        /// </summary>
        [Parameter]
        public bool Dot { get; set; }

        /// <summary>
        /// Set offset of the badge dot, like (left, top)
        /// </summary>
        [Parameter]
        public (int Left, int Top) Offset { get; set; }

        /// <summary>
        /// Max count to show
        /// </summary>
        [Parameter]
        public int OverflowCount
        {
            get => _overflowCount;
            set
            {
                if (_overflowCount == value)
                {
                    return;
                }

                _overflowCount = value;

                if (_overflowCount > 0)
                {
                    GenerateMaxNumberArray();
                }
            }
        }

        /// <summary>
        /// Whether to show badge when count is zero
        /// </summary>
        [Parameter]
        public bool ShowZero { get; set; } = false;

        /// <summary>
        /// Set Badge as a status dot
        /// </summary>
        [Parameter]
        public string Status { get; set; }

        /// <summary>
        /// If status is set, text sets the display text of the status dot
        /// </summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Text to show when hovering over the badge
        /// </summary>
        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string Size { get; set; }

        /// <summary>
        /// Wrapping this item.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        private ClassMapper CountClassMapper { get; set; } = new ClassMapper();

        private int[] _countArray = Array.Empty<int>();

        private readonly int[] _countSingleArray = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        private char[] _maxNumberArray = Array.Empty<char>();

        private string StatusOrPresetColor => Status.IsIn(_badgeStatusTypes) ? Status : (PresetColor.HasValue ? Enum.GetName(typeof(PresetColor), PresetColor) : "");

        private bool HasStatusOrColor => !string.IsNullOrWhiteSpace(Status) || !string.IsNullOrWhiteSpace(Color) || PresetColor.HasValue;

        private string CountStyle => Offset == default ? "" : $"{$"right:{-Offset.Left}px"};{$"margin-top:{Offset.Top}px"};";

        private string DotColorStyle => (PresetColor == null && !string.IsNullOrWhiteSpace(Color) ? $"background:{Color};" : "");

        private bool RealShowSup => (this.Dot && (!this.Count.HasValue || (this.Count > 0 || this.Count == 0 && this.ShowZero)))
                                || this.Count > 0
                                || (this.Count == 0 && this.ShowZero);

        private bool _dotEnter;

        private bool _dotLeave;

        private bool _finalShowSup;
        private int? _count;
        private int _overflowCount = 99;
        private bool _showOverflowCount = false;

        /// <summary>
        /// Sets the default CSS classes.
        /// </summary>
        private void SetClassMap()
        {
            var prefixName = "ant-badge";
            ClassMapper.Clear()
                .Add(prefixName)
                .If($"{prefixName}-status", () => HasStatusOrColor)
                .If($"{prefixName}-not-a-wrapper", () => ChildContent == null)
                .If($"{prefixName}-rtl", () => RTL)
                ;

            CountClassMapper.Clear()
                .Add("ant-scroll-number")
                .If($"{prefixName}-count", () => !Dot && !HasStatusOrColor)
                .If($"{prefixName}-dot", () => Dot || HasStatusOrColor)
                .If($"{prefixName}-count-sm", () => !string.IsNullOrWhiteSpace(Size) && Size.Equals("small", StringComparison.OrdinalIgnoreCase))
                .GetIf(() => $"ant-badge-status-{StatusOrPresetColor}", () => !string.IsNullOrWhiteSpace(StatusOrPresetColor))
                .If($"{prefixName}-multiple-words", () => _countArray.Length >= 2)
                .If($"{prefixName}-zoom-enter {prefixName}-zoom-enter-active", () => _dotEnter)
                .If($"{prefixName}-zoom-leave {prefixName}-zoom-leave-active", () => _dotLeave)
                .If($"{prefixName}-count-overflow", () => _showOverflowCount)
                ;
        }

        /// <summary>
        /// Startup code
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            _finalShowSup = RealShowSup;

            GenerateMaxNumberArray();

            SetClassMap();
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var showSupBefore = RealShowSup;
            var beforeDot = Dot;
            var delayDot = false;
            var beforeCount = _count;

            await base.SetParametersAsync(parameters);

            // if count is changing to 0 and it was overflow, hold the overflow display.
            if (_count == 0 && beforeCount > _overflowCount)
            {
                _showOverflowCount = true;
            }
            else
            {
                _showOverflowCount = _count > _overflowCount;
            }

            if (showSupBefore == RealShowSup)
                return;

            if (RealShowSup)
            {
                _dotEnter = true;

                if (!beforeDot && Dot)
                {
                    Dot = true;
                }

                _finalShowSup = true;

                await Task.Delay(200);
                _dotEnter = false;
            }
            else
            {
                _dotLeave = true;

                if (beforeDot && !Dot)
                {
                    delayDot = true;
                    Dot = true;
                }

                await Task.Delay(200);
                _dotLeave = false;
                _finalShowSup = false;

                if (delayDot)
                {
                    Dot = false;
                }
            }

            StateHasChanged();
        }

        private void GenerateMaxNumberArray()
        {
            this._maxNumberArray = _overflowCount.ToString(CultureInfo.InvariantCulture).ToCharArray();
        }

        private static int[] DigitsFromInteger(int number)
        {
            var n = Math.Abs(number);
            var length = (int)Math.Log10(n > 0 ? n : 1) + 1;
            var digits = new int[length];
            for (var i = 0; i < length; i++)
            {
                digits[length - i - 1] = n % 10;
                n /= 10;
            }

            if (n < 0)
                digits[0] *= -1;

            return digits;
        }

        private static readonly string[] _badgeStatusTypes =
        {
            "success",
            "processing",
            "default",
            "error",
            "warning"
        };
    }
}
