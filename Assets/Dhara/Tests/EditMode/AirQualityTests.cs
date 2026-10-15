using NUnit.Framework;
using System;
namespace Dhara.Tests
{
    public class AirQualityTests
    {
        [Test] public void PollutantsKeepNullSeparateFromZero()
        {var d=AirQualityData.Parse("{\"utc_offset_seconds\":19800,\"current_units\":{\"pm2_5\":\"μg/m³\"},\"current\":{\"time\":\"2026-10-01T12:00\",\"pm2_5\":0,\"pm10\":null}}");Assert.That(d.Pm25,Is.EqualTo(0));Assert.That(d.Pm10,Is.Null);}
        [Test] public void WrongUnitsAndNegativePollutantsAreRejected()
        {Assert.Throws<FormatException>(()=>AirQualityData.Parse("{\"utc_offset_seconds\":19800,\"current_units\":{\"pm2_5\":\"mg/m3\"},\"current\":{\"time\":\"2026-10-01T12:00\",\"pm2_5\":4}}"));}
    }
}
