﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinearDelayRetryStrategyTest.cs">
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
    public class LinearDelayRetryStrategyTest
    {
        private readonly Random random = new Random();

        [Test]
        public void Constructor_assigns_arguments_to_properties()
        {
            var expectedScaleFactor = this.random.NextDouble();
            var expectedMaxRetries = this.random.Next();
            var expectedDelay = TimeSpan.FromMilliseconds(this.random.Next());
            var retryStrategy = new LinearDelayRetryStrategy(expectedScaleFactor, expectedMaxRetries, expectedDelay);

            Assert.That(retryStrategy.ScaleFactor, Is.EqualTo(expectedScaleFactor));
            Assert.That(retryStrategy.MaxRetries, Is.EqualTo(expectedMaxRetries));
            Assert.That(retryStrategy.Delay, Is.EqualTo(expectedDelay));
        }

        [Test]
        public void Constructor_assigns_default_properties()
        {
            var expectedScaleFactor = this.random.NextDouble();
            var retryStrategy = new LinearDelayRetryStrategy(expectedScaleFactor);

            Assert.That(retryStrategy.ScaleFactor, Is.EqualTo(expectedScaleFactor));
            Assert.That(retryStrategy.MaxRetries, Is.EqualTo(TryDefault.MaxRetries));
            Assert.That(retryStrategy.Delay, Is.EqualTo(TryDefault.Delay));
        }

        [Test]
        public void ShouldQuit_returns_false_when_attempt_less_than_or_equal_MaxRetries()
        {
            var retryStrategy = new LinearDelayRetryStrategy(1);

            Assert.That(retryStrategy.ShouldQuit(TryDefault.MaxRetries - 1), Is.False);
            Assert.That(retryStrategy.ShouldQuit(TryDefault.MaxRetries), Is.False);
        }

        [Test]
        public void ShouldQuit_returns_true_when_attempt_greater_than_MaxRetries()
        {
            var retryStrategy = new LinearDelayRetryStrategy(1);

            Assert.That(retryStrategy.ShouldQuit(TryDefault.MaxRetries + 1), Is.True);
        }

        [Test]
        public void Wait_calls_calculate_with_expected_values()
        {
            var wasCalled = false;
            var expectedAttempt = this.random.Next(1, int.MaxValue);
            var expectedScaleFactor = this.random.NextDouble();
            var retryStrategy = new LinearDelayRetryStrategy(expectedScaleFactor);
            retryStrategy.Delay = TimeSpan.FromMilliseconds(this.random.Next(1, int.MaxValue));

            retryStrategy.calculateSleepTime = (attempt, delay, scaleFactor) =>
            {
                wasCalled = true;

                Assert.That(attempt, Is.EqualTo(expectedAttempt));
                Assert.That(delay, Is.EqualTo(retryStrategy.Delay));
                Assert.That(scaleFactor, Is.EqualTo(expectedScaleFactor));

                return TimeSpan.FromMilliseconds(1); // This will cause a delay in test execution - keep value tiny
            };

            retryStrategy.Wait(expectedAttempt);
            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void Wait_sleeps_for_the_time_returned_from_calculateSleepTime()
        {
            var retryStrategy = new LinearDelayRetryStrategy(1);

            // This will cause a delay in test execution - keep value tiny
            retryStrategy.calculateSleepTime = (_, __, ___) => TimeSpan.FromMilliseconds(50);

            var stopWatch = Stopwatch.StartNew();
            retryStrategy.Wait(1);
            stopWatch.Stop();

            // Stopwatch is pretty accurate, but test has possibility of other system delays introducing extra
            // delays and 50 milliseconds is pretty tiny so being generous on the high side
            Assert.That(stopWatch.ElapsedMilliseconds, Is.InRange(50, 100));
        }

        [Test]
        public void CalculateSleepTime_returns_expected_delays()
        {
            var delay = TimeSpan.FromMilliseconds(10);

            Assert.That(LinearDelayRetryStrategy.CalculateSleepTime(1, delay, 10).TotalMilliseconds, Is.EqualTo(10));
            Assert.That(LinearDelayRetryStrategy.CalculateSleepTime(2, delay, 10).TotalMilliseconds, Is.EqualTo(20));
            Assert.That(LinearDelayRetryStrategy.CalculateSleepTime(3, delay, 10).TotalMilliseconds, Is.EqualTo(30));
            Assert.That(LinearDelayRetryStrategy.CalculateSleepTime(10, delay, 10).TotalMilliseconds, Is.EqualTo(100));
        }

        [Test]
        public void CalculateSleepTime_ensures_delay_is_at_least_1()
        {
            var actualDelay = LinearDelayRetryStrategy.CalculateSleepTime(1, TimeSpan.Zero, -1);

            Assert.That(actualDelay.TotalMilliseconds, Is.EqualTo(1));
        }
    }
}
