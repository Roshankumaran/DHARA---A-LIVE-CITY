using System;
using NUnit.Framework;
namespace Dhara.Tests
{
    public class ScenarioReportTests
    {
        DistrictComparison Example(){var e=new DistrictExperiment();e.Capture(new WeatherData{Time="2026-10-01T12:00",RainMmPerHour=50,Temperature=25,WindKmh=20,CloudPercent=70},12,"WhatIf","Bengaluru",null);return e.Compare(new InterventionConfig{GreenRoofFraction=.5,PermeableFraction=.5,GardenCapacityM3=10});}
        [Test] public void SnapshotRoundTripRecalculatesAndClassifiesAsRecorded()
        {var c=Example();var json=ScenarioReport.Serialize(c,"Test \"quoted\"\nname");var saved=ScenarioReport.Parse(json);Assert.That(saved.Comparison.Classification,Is.EqualTo("Recorded"));Assert.That(saved.OriginalClassification,Is.EqualTo("WhatIf"));Assert.That(saved.Comparison.Intervention.StorageM3,Is.EqualTo(c.Intervention.StorageM3));Assert.That(saved.Comparison.Input.SolarWm2,Is.Null);}
        [Test] public void WeatherCodeAndIntervalSurviveSnapshotRoundTrip()
        {var c=Example();c.Input.WeatherCode=61;c.Input.IntervalSeconds=900;var saved=ScenarioReport.Parse(ScenarioReport.Serialize(c,"Weather metadata"));Assert.That(saved.Comparison.Input.WeatherCode,Is.EqualTo(61));Assert.That(saved.Comparison.Input.IntervalSeconds,Is.EqualTo(900));Assert.That(ScenarioReport.Csv(c),Does.Contain("weatherCode"));}
        [Test] public void IncompatibleOrTamperedSnapshotsAreRejected()
        {string json=ScenarioReport.Serialize(Example(),"test");Assert.Throws<FormatException>(()=>ScenarioReport.Parse(json.Replace("\"schemaVersion\":1","\"schemaVersion\":99")));Assert.Throws<FormatException>(()=>ScenarioReport.Parse(json.Replace("\"rainMmHour\":50","\"rainMmHour\":500")));Assert.Throws<FormatException>(()=>ScenarioReport.Parse("not json"));}
        [Test] public void CsvContainsCalculatedMetricsAndUnavailableValues()
        {var c=Example();string csv=ScenarioReport.Csv(c);Assert.That(csv,Does.Contain(c.Intervention.StorageM3.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));Assert.That(csv,Does.Contain("Unavailable"));Assert.That(csv,Does.Contain("absolute_difference,percent_difference"));}
    }
}
