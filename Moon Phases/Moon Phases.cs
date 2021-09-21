using System;
using cAlgo.API;
using System.Collections.Generic;
using System.Linq;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MoonPhases : Indicator
    {
        private Color _newMoonColor, _waxingCrescentColor, _firstQuarterColor, _waxingGibbousColor;
        private Color _fullMoonColor, _waningGibbousColor, _thirdQuarterColor, _waningCrescentColor;

        private Moon.PhaseResult _lastPhase;

        private ChartRectangle _lastPhaseRectangle;

        [Parameter("Hemispheres", DefaultValue = Moon.Hemispheres.Northern, Group = "General")]
        public Moon.Hemispheres Hemispheres { get; set; }

        [Parameter("New Moon", DefaultValue = "#a89932", Group = "Colors")]
        public string NewMoonColor { get; set; }

        [Parameter("Waxing Crescent", DefaultValue = "#8ba832", Group = "Colors")]
        public string WaxingCrescentColor { get; set; }

        [Parameter("First Quarter", DefaultValue = "#69a832", Group = "Colors")]
        public string FirstQuarterColor { get; set; }

        [Parameter("Waxing Gibbous", DefaultValue = "#32a88e", Group = "Colors")]
        public string WaxingGibbousColor { get; set; }

        [Parameter("Full Moon", DefaultValue = "#1432a8", Group = "Colors")]
        public string FullMoonColor { get; set; }

        [Parameter("Waning Gibbous", DefaultValue = "#55329c", Group = "Colors")]
        public string WaningGibbousColor { get; set; }

        [Parameter("Third Quarter", DefaultValue = "#7b1a99", Group = "Colors")]
        public string ThirdQuarterColor { get; set; }

        [Parameter("Waning Crescent", DefaultValue = "#ad0e46", Group = "Colors")]
        public string WaningCrescentColor { get; set; }

        [Parameter("Alpha", DefaultValue = 100, MinValue = 1, MaxValue = 255, Group = "Colors")]
        public int Alpha { get; set; }

        protected override void Initialize()
        {
            _newMoonColor = GetColor(NewMoonColor, Alpha);
            _waxingCrescentColor = GetColor(WaxingCrescentColor, Alpha);
            _firstQuarterColor = GetColor(FirstQuarterColor, Alpha);
            _waxingGibbousColor = GetColor(WaxingGibbousColor, Alpha);
            _fullMoonColor = GetColor(FullMoonColor, Alpha);
            _waningGibbousColor = GetColor(WaningGibbousColor, Alpha);
            _thirdQuarterColor = GetColor(ThirdQuarterColor, Alpha);
            _waningCrescentColor = GetColor(WaningCrescentColor, Alpha);
        }

        public override void Calculate(int index)
        {
            var phase = Moon.Calculate(Bars.OpenTimes[index], Moon.Hemispheres.Northern);

            if (_lastPhase == null || phase.Name.Equals(_lastPhase.Name, StringComparison.OrdinalIgnoreCase) == false)
            {
                _lastPhase = phase;

                var rectangleName = string.Format("moon_phase_{0}", index);
                var rectangleColor = GetPhaseColor(phase.Name);

                _lastPhaseRectangle = Chart.DrawRectangle(rectangleName, Bars.OpenTimes[index], Chart.TopY + Chart.TopY * 100, Bars.OpenTimes[index], Chart.BottomY - Chart.BottomY * 100, rectangleColor);
                _lastPhaseRectangle.IsFilled = true;
            }
            else
            {
                _lastPhaseRectangle.Time2 = Bars.OpenTimes[index];
            }
        }

        private Color GetPhaseColor(string phaseName)
        {
            switch (phaseName)
            {
                case Moon.Phase.NewMoon:
                    return _newMoonColor;

                case Moon.Phase.WaxingCrescent:
                    return _waxingCrescentColor;

                case Moon.Phase.FirstQuarter:
                    return _firstQuarterColor;

                case Moon.Phase.WaxingGibbous:
                    return _waxingGibbousColor;

                case Moon.Phase.FullMoon:
                    return _fullMoonColor;

                case Moon.Phase.WaningGibbous:
                    return _waningGibbousColor;

                case Moon.Phase.ThirdQuarter:
                    return _thirdQuarterColor;

                case Moon.Phase.WaningCrescent:
                    return _waningCrescentColor;

                default:
                    throw new ArgumentOutOfRangeException("phaseName");
            }
        }

        private Color GetColor(string colorString, int alpha = 255)
        {
            var color = colorString[0] == '#' ? Color.FromHex(colorString) : Color.FromName(colorString);

            return Color.FromArgb(alpha, color);
        }
    }

    /// <summary>
    /// This is a copy of https://github.com/khalidabuhakmeh/MoonPhaseConsole
    /// Special thanks for Khalid Abuhakmeh for his great work!
    /// </summary>
    public static class Moon
    {
        private static readonly List<string> _northernHemisphere = new List<string> { "🌑", "🌒", "🌓", "🌔", "🌕", "🌖", "🌗", "🌘", "🌑" };

        private static readonly List<string> _southernHemisphere = _northernHemisphere.ToArray().Reverse().ToList();

        private static readonly List<string> _names = new List<string>
        {
            Phase.NewMoon,
            Phase.WaxingCrescent,
            Phase.FirstQuarter,
            Phase.WaxingGibbous,
            Phase.FullMoon,
            Phase.WaningGibbous,
            Phase.ThirdQuarter,
            Phase.WaningCrescent
        };

        private const double _totalLengthOfCycle = 29.53;

        private static readonly List<Phase> _allPhases = new List<Phase>();

        static Moon()
        {
            var period = _totalLengthOfCycle / _names.Count;
            // divide the phases into equal parts
            // making sure there are no gaps
            _allPhases = _names
                .Select((t, i) => new Phase(t, period * i, period * (i + 1)))
                .ToList();
        }

        /// <summary>
        /// Calculate the current phase of the moon.
        /// Note: this calculation uses the last recorded new moon to calculate the cycles of
        /// of the moon since then. Any date in the past before 1920 might not work.
        /// </summary>
        /// <param name="utcDateTime"></param>
        /// <remarks>https://www.subsystems.us/uploads/9/8/9/4/98948044/moonphase.pdf</remarks>
        /// <returns></returns>
        public static PhaseResult Calculate(DateTime utcDateTime, Hemispheres viewFromEarth = Hemispheres.Northern)
        {
            const double julianConstant = 2415018.5;
            var julianDate = utcDateTime.ToOADate() + julianConstant;

            // London New Moon (1920)
            // https://www.timeanddate.com/moon/phases/uk/london?year=1920
            var daysSinceLastNewMoon =
                new DateTime(1920, 1, 21, 5, 25, 00, DateTimeKind.Utc).ToOADate() + julianConstant;

            var newMoons = (julianDate - daysSinceLastNewMoon) / _totalLengthOfCycle;
            var intoCycle = (newMoons - Math.Truncate(newMoons)) * _totalLengthOfCycle;

            var phase = _allPhases.First(p => intoCycle >= p.Start && intoCycle <= p.End);

            var index = _allPhases.IndexOf(phase);

            return new PhaseResult
            (
                phase.Name,
                viewFromEarth == Hemispheres.Northern ? _northernHemisphere[index] : _southernHemisphere[index],
                Math.Round(intoCycle, 2),
                viewFromEarth,
                utcDateTime
            );
        }

        public static PhaseResult UtcNow(Hemispheres viewFromEarth = Hemispheres.Northern)
        {
            return Calculate(DateTime.UtcNow, viewFromEarth);
        }

        public static PhaseResult Now(Hemispheres viewFromEarth = Hemispheres.Northern)
        {
            return Calculate(DateTime.Now.ToUniversalTime(), viewFromEarth);
        }

        public class PhaseResult
        {
            public PhaseResult(string name, string emoji, double daysIntoCycle, Hemispheres hemisphere,
                DateTime moment)
            {
                Name = name;
                Emoji = emoji;
                DaysIntoCycle = daysIntoCycle;
                Hemisphere = hemisphere;
                Moment = moment;
            }

            public string Name { get; set; }
            public string Emoji { get; set; }
            public double DaysIntoCycle { get; set; }
            public Hemispheres Hemisphere { get; set; }
            public DateTime Moment { get; set; }

            public double Visibility
            {
                get
                {
                    const int FullMoon = 15;
                    const double halfCycle = _totalLengthOfCycle / 2;

                    var numerator = DaysIntoCycle > FullMoon
                        // past the full moon, we want to count down
                        ? halfCycle - (DaysIntoCycle % halfCycle)
                        // leading up to the full moon
                        : DaysIntoCycle;

                    return numerator / halfCycle * 100;
                }
            }
        }

        public class Phase
        {
            public const string NewMoon = "New Moon";
            public const string WaxingCrescent = "Waxing Crescent";
            public const string FirstQuarter = "First Quarter";
            public const string WaxingGibbous = "Waxing Gibbous";
            public const string FullMoon = "Full Moon";
            public const string WaningGibbous = "Waning Gibbous";
            public const string ThirdQuarter = "Third Quarter";
            public const string WaningCrescent = "Waning Crescent";

            public Phase(string name, double start, double end)
            {
                Name = name;
                Start = start;
                End = end;
            }

            public string Name { get; set; }

            /// <summary>
            /// The days into the cycle this phase starts
            /// </summary>
            public double Start { get; set; }

            /// <summary>
            /// The days into the cycle this phase ends
            /// </summary>
            public double End { get; set; }
        }

        public enum Hemispheres
        {
            Northern,
            Southern
        }
    }
}