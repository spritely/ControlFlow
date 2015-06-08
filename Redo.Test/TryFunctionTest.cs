﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TryFunctionTest.cs">
//   Copyright (c) 2015. All rights reserved.
//   Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Redo.Test
{
    using System;
    using System.Linq;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class TryFunctionTest
    {
        [SetUp]
        public void Initialize()
        {
            // Replace the default retry strategy with a test instance that doesn't delay and quits after 50 tries
            var retryStrategy = new Mock<IRetryStrategy>();

            // false, false, false...., true (when ran 50 times)
            retryStrategy.Setup(s => s.ShouldQuit(It.IsAny<long>())).Returns<long>(attempt => attempt > 50);

            TryDefault.RetryStrategy = retryStrategy.Object;
        }

        [TearDown]
        public void Cleanup()
        {
            TryDefault.Reset();
        }

        [Test]
        public void Running_throws_on_null_argument()
        {
            Assert.Throws<ArgumentNullException>(() => Try.Running(null as Func<object>));
        }

        // TryFunctionTest validates shared functional paths between TryAction and TryFunction.
        // These next two methods ensure the TryFunction methods call the same underlying functionality.
        [Test]
        public void until_defaults_to_UntilExtension_Until()
        {
            var tryFunction = Try.Running(() => true);

            Assert.That(tryFunction.until == Run.Until);
        }

        [Test]
        public void Until_calls_until_with_expected_parameters()
        {
            var expectedResult = new object();
            Func<object, bool> expectedSatisfied = _ => true;
            var expectedConfiguration = new TryConfiguration();

            Func<object, bool> actualSatisfied = null;
            TryConfiguration actualConfiguration = null;

            var tryAction = Try.Running(() => expectedResult);
            tryAction.configuration = expectedConfiguration;
            tryAction.until = (f, satisfied, configuration) =>
            {
                actualSatisfied = satisfied;
                actualConfiguration = configuration;
                return f();
            };

            var actualResult = tryAction.Until(expectedSatisfied);

            Assert.That(actualResult, Is.SameAs(expectedResult));
            Assert.That(actualSatisfied, Is.SameAs(expectedSatisfied));
            Assert.That(actualConfiguration, Is.SameAs(expectedConfiguration));
        }

        [Test]
        public void UntilNotNull_calls_until_with_expected_parameters()
        {
            var expectedResult = new object();
            var expectedConfiguration = new TryConfiguration();

            Func<object, bool> actualSatisfied = null;
            TryConfiguration actualConfiguration = null;

            var tryAction = Try.Running(() => expectedResult);
            tryAction.configuration = expectedConfiguration;
            tryAction.until = (f, satisfied, configuration) =>
            {
                actualSatisfied = satisfied;
                actualConfiguration = configuration;
                return f();
            };

            var actualResult = tryAction.UntilNotNull();

            Assert.That(actualResult, Is.SameAs(expectedResult));
            Assert.That(actualConfiguration, Is.SameAs(expectedConfiguration));
            Assert.That(actualSatisfied(null), Is.False);
            Assert.That(actualSatisfied(new object()), Is.True);
            Assert.That(actualSatisfied(1), Is.True);
        }

        [Test]
        public void Until_uses_default_retry_strategy()
        {
            var retryStrategy = new Mock<IRetryStrategy>();
            TryDefault.RetryStrategy = retryStrategy.Object;

            // On exception ShouldQuit() = false so code will reach Wait()
            retryStrategy.Setup(s => s.ShouldQuit(It.IsAny<long>())).Returns(false);

            // Run twice to ensure Until() reaches Wait()
            var i = 0;
            Try.Running<object>(() => { throw new Exception(); })
                .Until(_ => i++ >= 1);

            retryStrategy.Verify(s => s.ShouldQuit(It.IsAny<long>()), Times.AtLeastOnce);
            retryStrategy.Verify(s => s.Wait(It.IsAny<long>()), Times.AtLeastOnce);
        }

        [Test]
        public void With_sets_the_retry_strategy()
        {
            var retryStrategy = new Mock<IRetryStrategy>();

            // On exception ShouldQuit() = false so code will reach Wait()
            retryStrategy.Setup(s => s.ShouldQuit(It.IsAny<long>())).Returns(false);

            // Run twice to ensure Until() reaches Wait()
            var i = 0;
            Try.Running<object>(() => { throw new Exception(); })
                .With(retryStrategy.Object)
                .Until(_ => i++ >= 1);

            retryStrategy.Verify(s => s.ShouldQuit(It.IsAny<long>()), Times.AtLeastOnce);
            retryStrategy.Verify(s => s.Wait(It.IsAny<long>()), Times.AtLeastOnce);
        }

        [Test]
        public void RetryStrategy_Wait_is_called_with_current_1_based_attempt_value()
        {
            var retryStrategy = new Mock<IRetryStrategy>();
            var i = 0;

            retryStrategy.Setup(s => s.Wait(i + 1)).Verifiable();

            Try.Running<object>(() => { throw new Exception(); })
                .With(retryStrategy.Object)
                .Until(_ => i++ >= 10);

            retryStrategy.Verify();
        }

        [Test]
        public void Until_returns_result_of_successful_call()
        {
            var retryStrategy = new Mock<IRetryStrategy>();

            var expected = new object();
            var actual = Try.Running(() => expected)
                .With(retryStrategy.Object)
                .Until(_ => true);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Until_retries_until_Until_returns_true()
        {
            var retryStrategy = new Mock<IRetryStrategy>();
            TryDefault.RetryStrategy = retryStrategy.Object;

            // false, false, false...., true
            var falseCount = new Random().Next(2, 10);
            var untilReturns = Enumerable.Range(0, falseCount).Select(i => false).Concat(new[] { true }).ToList();
            var calls = 0;

            Try.Running<object>(() => { throw new Exception(); })
                .With(retryStrategy.Object)
                .Until(_ => untilReturns[calls++]);

            Assert.That(calls, Is.EqualTo(falseCount + 1));
        }

        [Test]
        public void Until_retries_until_ShouldQuit_returns_true()
        {
            var retryStrategy = new Mock<IRetryStrategy>();

            // false, false, false...., true
            var falseCount = new Random().Next(2, 10);
            var calls = 0;
            retryStrategy.Setup(s => s.ShouldQuit(It.IsAny<long>())).Returns<long>(attempt =>
            {
                calls++;
                return attempt > falseCount;
            });

            var times = 0;

            Assert.Throws<Exception>(() =>
                Try.Running<object>(() => { throw new Exception(); })
                    .With(retryStrategy.Object)
                    .Until(_ => times++ >= (falseCount + 2))); // No infinite loop on test failure

            Assert.That(calls, Is.EqualTo(falseCount + 1));
        }

        [Test]
        public void Wait_is_not_called_after_ShouldQuit_returns_true()
        {
            var retryStrategy = new Mock<IRetryStrategy>();

            retryStrategy.Setup(s => s.ShouldQuit(It.IsAny<long>())).Returns(true);

            var i = 0;
            Assert.Throws<Exception>(() =>
                Try.Running<object>(() => { throw new Exception(); })
                    .With(retryStrategy.Object)
                    .Until(_ => i++ >= 2)); // Do not return via Until on the first attempt

            retryStrategy.Verify(s => s.Wait(It.IsAny<long>()), Times.Never);
        }

        [Test]
        public void Wait_is_not_called_when_Until_returns_true()
        {
            var retryStrategy = new Mock<IRetryStrategy>();

            Try.Running<object>(() => { throw new Exception(); })
                .With(retryStrategy.Object)
                .Until(_ => true);

            retryStrategy.Verify(s => s.Wait(It.IsAny<long>()), Times.Never);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Verifying the system handles any Exception type including the base Exception type.")]
        [Test]
        public void Until_rethrows_original_exception_when_ShouldQuit_returns_true()
        {
            var retryStrategy = new Mock<IRetryStrategy>();

            retryStrategy.Setup(s => s.ShouldQuit(It.IsAny<long>())).Returns(true);

            var expectedException = new Exception();
            var i = 0;
            try
            {
                Try.Running<object>(() => { throw expectedException; })
                    .With(retryStrategy.Object)
                    .Until(_ => i++ >= 2); // No infinite loop on test failure
            }
            catch (Exception actualException)
            {
                Assert.That(actualException, Is.SameAs(expectedException));
            }
        }

        [Test]
        public void Until_reports_exceptions_to_default_delegates()
        {
            var expectedException = new Exception();
            Exception actualException = null;
            TryDefault.ExceptionListeners += ex => actualException = ex;

            Try.Running<object>(() => { throw expectedException; })
                .Until(_ => true);

            Assert.That(actualException, Is.SameAs(expectedException));
        }

        [Test]
        public void Until_reports_exceptions_to_call_specific_delegates()
        {
            var expectedException = new Exception();
            Exception actualException = null;

            Try.Running<object>(() => { throw expectedException; })
                .Report(ex => actualException = ex)
                .Until(_ => true);

            Assert.That(actualException, Is.SameAs(expectedException));
        }

        [Test]
        public void Until_defaults_to_handling_Exception_when_no_default_handlers_specified()
        {
            Try.Running<object>(() => { throw new Exception(); })
                .Until(_ => true);
        }

        [Test]
        public void Until_uses_default_exception_handler_when_Handle_not_called()
        {
            TryDefault.AddHandle<TestException1>();

            Try.Running<object>(() => { throw new TestException1(); })
                .Until(_ => true);

            Assert.Throws<Exception>(() =>
                Try.Running<object>(() => { throw new Exception(); })
                    .Until(_ => true));
        }

        [Test]
        public void Until_handles_exceptions_specified_with_Handle()
        {
            Try.Running<object>(() => { throw new TestException1(); })
                .Handle<TestException1>()
                .Until(_ => true);
        }

        [Test]
        public void Until_propagates_exceptions_not_specified_with_Handle()
        {
            Assert.Throws<Exception>(() =>
                Try.Running<object>(() => { throw new Exception(); })
                    .Handle<TestException1>()
                    .Until(_ => true));
        }

        [Test]
        public void Until_handles_multiple_exception_types_specified_with_Handle()
        {
            var retryStrategy = new Mock<IRetryStrategy>();
            TryDefault.RetryStrategy = retryStrategy.Object;

            var i = 0;
            Try.Running<object>(() =>
            {
                if (i == 0)
                {
                    throw new TestException1();
                }

                throw new TestException2();
            })
                .Handle<TestException1>()
                .Handle<TestException2>()
                .Until(_ => i++ >= 2);
        }

        [Test]
        public void Until_propagates_exceptions_not_specified_with_multiple_Handle_calls()
        {
            var retryStrategy = new Mock<IRetryStrategy>();
            TryDefault.RetryStrategy = retryStrategy.Object;

            var i = 0;
            Assert.Throws<TestException3>(() =>
                Try.Running<object>(() =>
                {
                    if (i == 0)
                    {
                        throw new TestException1();
                    }

                    throw new TestException3();
                })
                    .Handle<TestException1>()
                    .Handle<TestException2>()
                    .Until(_ => i++ >= 2));
        }

        [Serializable]
        private class TestException1 : Exception
        {
        }

        [Serializable]
        private class TestException2 : Exception
        {
        }

        [Serializable]
        private class TestException3 : Exception
        {
        }
    }
}
