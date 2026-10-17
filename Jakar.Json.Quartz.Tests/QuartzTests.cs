// Jakar.Json.Quartz.Tests
// 10/17/2026

using Quartz;
using Quartz.Impl;



namespace Jakar.Json.Quartz.Tests;


[TestFixture]
public sealed class QuartzTests
{
    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Put_StoresJakarJsonText_GetReadsIt()
    {
        JobDataMap map = new JobDataMap().PutJakarJson("order", Samples.Order()).PutJakarJson("orders", Samples.Orders());

        Assert.Multiple(() =>
                        {
                            Assert.That(map.GetString("order"),                  Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(map.GetJakarJson<Order>("order"),        Is.EqualTo(Samples.Order()));
                            Assert.That(map.GetJakarJson<List<Order>>("orders"), Is.EqualTo(Samples.Orders()));
                            Assert.That(map.GetJakarJson<Order>("missing"),      Is.Null);
                        });
    }

    [Test] public void NonText_Throws()
    {
        JobDataMap map = new() { ["count"] = 3 };
        Assert.Throws<InvalidCastException>(() => map.GetJakarJson<Order>("count"));
    }

    [Test] public void JobBuilder_AddsTheModel()
    {
        IJobDetail job = JobBuilder.Create<NoOpJob>().WithIdentity("j").UsingJakarJson("order", Samples.Order()).Build();
        Assert.That(job.JobDataMap.GetJakarJson<Order>("order"), Is.EqualTo(Samples.Order()));
    }


    // ─── Integration: what a persistent job store does with the data ─────────

    [Test] public void SurvivesQuartzsJsonSerializer()
    {
        SystemTextJsonObjectSerializer serializer = new();
        JobDataMap                     map        = new JobDataMap { ["attempt"] = 2 }.PutJakarJson("order", Samples.Order());

        JobDataMap restored = serializer.Deserialize<JobDataMap>(serializer.Serialize(map))!;

        Assert.Multiple(() =>
                        {
                            Assert.That(restored.GetJakarJson<Order>("order"), Is.EqualTo(Samples.Order()));
                            Assert.That(restored.GetInt("attempt"),            Is.EqualTo(2));
                        });
    }



    public sealed class NoOpJob : IJob
    {
        public ValueTask Execute( IJobExecutionContext context, CancellationToken cancellationToken = default ) => ValueTask.CompletedTask;
    }
}
