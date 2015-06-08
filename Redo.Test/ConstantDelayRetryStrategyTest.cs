﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleDelayRetryStrategyTest.cs">
//   Copyright (c) 2015. All rights reserved.
//   Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Redo.Test
{
    using System;
    using System.Diagnostics;
    using NUnit.Framework;

    [TestFixture]
    public class ConstantDelayRetryStrategyTest
    {
        private readonly Random random = new Random();

        [Test]
        public void Constructor_assigns_arguments_to_properties()
        {
            var expectedMaxRetries = this.random.Next();
            var expectedDelay = TimeSpan.FromMilliseconds(this.random.Next());
            var retryStrategy = new ConstantDelayRetryStrategy(expectedMaxRetries, expectedDelay);

            Assert.That(retryStrategy.MaxRetries, Is.EqualTo(expectedMaxRetries));
            Assert.That(retryStrategy.Delay, Is.EqualTo(expectedDelay));
        }

        [Test]
        public void Constructor_assigns_default_properties()
        {
            var retryStrategy = new ConstantDelayRetryStrategy();

            Assert.That(retryStrategy.MaxRetries, Is.EqualTo(TryDefault.MaxRetries));
            Assert.That(retryStrategy.Delay, Is.EqualTo(TryDefault.Delay));
        }

        [Test]
        public void ShouldQuit_returns_false_when_attempt_less_than_or_equal_MaxRetries()
        {
            var retryStrategy = new ConstantDelayRetryStrategy();

            Assert.That(retryStrategy.ShouldQuit(TryDefault.MaxRetries - 1), Is.False);
            Assert.That(retryStrategy.ShouldQuit(TryDefault.MaxRetries), Is.False);
        }

        [Test]
        public void ShouldQuit_returns_true_when_attempt_greater_than_MaxRetries()
        {
            var retryStrategy = new ConstantDelayRetryStrategy();

            Assert.That(retryStrategy.ShouldQuit(TryDefault.MaxRetries + 1), Is.True);
        }

        [Test]
        public void Wait_calls_calculate_with_Delay()
        {
            var wasCalled = false;
            var retryStrategy = new ConstantDelayRetryStrategy();
            retryStrategy.Delay = TimeSpan.FromMilliseconds(this.random.Next(1, int.MaxValue));

            retryStrategy.calculateSleepTime = delay =>
            {
                wasCalled = true;
                Assert.That(delay, Is.EqualTo(retryStrategy.Delay));
                return TimeSpan.FromMilliseconds(1); // This will cause a delay in test execution - keep value tiny
            };

            retryStrategy.Wait(1);
            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void Wait_sleeps_for_the_time_returned_from_calculateSleepTime()
        {
            var retryStrategy = new ConstantDelayRetryStrategy();

            // This will cause a delay in test execution - keep value tiny
            retryStrategy.calculateSleepTime = _ => TimeSpan.FromMilliseconds(50);

            var stopWatch = Stopwatch.StartNew();
            retryStrategy.Wait(1);
            stopWatch.Stop();

            // Stopwatch is pretty accurate, but test has possibility of other system delays introducing extra
            // delays and 50 milliseconds is pretty tiny so being generous on the high side
            Assert.That(stopWatch.ElapsedMilliseconds, Is.InRange(50, 100));
        }

        [Test]
        public void CalculateSleepTime_returns_the_assigned_Delay()
        {
            var expectedDelay = TimeSpan.FromMilliseconds(this.random.Next(1, int.MaxValue));

            var actualDelay = ConstantDelayRetryStrategy.CalculateSleepTime(expectedDelay);

            Assert.That(actualDelay, Is.EqualTo(expectedDelay));
        }

        [Test]
        public void CalculateSleepTime_ensures_delay_is_at_least_1()
        {
            var actualDelay = ConstantDelayRetryStrategy.CalculateSleepTime(TimeSpan.Zero);

            Assert.That(actualDelay.TotalMilliseconds, Is.EqualTo(1));
        }
    }
}
