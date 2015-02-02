﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TryTest.cs">
//   Copyright (c) 2015. All rights reserved.
//   Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Xunit;

namespace Spritely.Redo.Test
{
    public class TryTest
    {
        [Fact]
        public void Running_returns_a_TryAction_when_an_action_is_passed()
        {
            var actual = Try.Running(() => { });

            Assert.IsType<TryAction>(actual);
        }

        [Fact]
        public void Running_returns_a_TryFunction_when_a_function_is_passed()
        {
            var actual = Try.Running(() => new object());

            Assert.IsType<TryFunction<object>>(actual);
        }
    }
}
