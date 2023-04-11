﻿namespace MassTransit.Initializers.TypeConverters
{
    using System;


    public class DateTimeTypeConverter :
        ITypeConverter<string, DateTime>,
        ITypeConverter<int, DateTime>,
        ITypeConverter<long, DateTime>,
        ITypeConverter<DateTime, string>,
        ITypeConverter<DateTime, object>,
        ITypeConverter<DateTime, DateTimeOffset>,
        ITypeConverter<DateTime, int>,
        ITypeConverter<DateTime, long>
    {
        readonly DateTime _epoch = new DateTime(1970, 1, 1);

        public bool TryConvert(DateTimeOffset input, out DateTime result)
        {
            result = input.UtcDateTime;
            return true;
        }

        public bool TryConvert(int input, out DateTime result)
        {
            result = _epoch + TimeSpan.FromMilliseconds(input);
            return true;
        }

        public bool TryConvert(long input, out DateTime result)
        {
            result = _epoch + TimeSpan.FromMilliseconds(input);
            return true;
        }

        public bool TryConvert(object input, out DateTime result)
        {
            switch (input)
            {
                case DateTime dateTime:
                    result = dateTime;
                    return true;

                case DateTimeOffset dateTimeOffset:
                    result = dateTimeOffset.UtcDateTime;
                    return true;

                case string text when !string.IsNullOrWhiteSpace(text):
                    return TryConvert(text, out result);

                default:
                    result = default;
                    return false;
            }
        }

        public bool TryConvert(string input, out DateTime result)
        {
            if (DateTimeOffset.TryParse(input, out var value))
            {
                result = value.Offset == TimeSpan.Zero ? value.UtcDateTime : value.LocalDateTime;
                return true;
            }

            result = default;
            return false;
        }

        public bool TryConvert(DateTime input, out int result)
        {
            if (input >= _epoch)
            {
                var timeSpan = input - _epoch;
                if (timeSpan.TotalMilliseconds <= int.MaxValue)
                {
                    result = (int)timeSpan.TotalMilliseconds;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool TryConvert(DateTime input, out long result)
        {
            if (input >= _epoch)
            {
                var timeSpan = input - _epoch;
                if (timeSpan.TotalMilliseconds <= long.MaxValue)
                {
                    result = (long)timeSpan.TotalMilliseconds;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool TryConvert(DateTime input, out string result)
        {
            result = input.ToString("O");
            return true;
        }
    }
}
