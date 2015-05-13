// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.CommonTestUtils;
using Xunit;

namespace Microsoft.Framework.Runtime.FunctionalTests
{
    public class ProjectResolverTests
    {
        [Fact]
        public void ProjectResolverThrowsWhenResolvingAmbiguousName()
        {
            const string ambiguousName = "ProjectA";
            var solutionStructure = @"{
  'global.json': '',
  'src1': {
    'ProjectA': {
      'project.json': '{}'
    }
  },
  'src2': {
    'ProjectA': {
      'project.json': '{}'
    }
  }
}";

            using (var solutionPath = new DisposableDir())
            {
                DirTree.CreateFromJson(solutionStructure)
                    .WithFileContents("global.json", @"{
  ""projects"": [""src1"", ""src2""]
}")
                    .WriteTo(solutionPath);

                var src1ProjectPath = Path.Combine(solutionPath, "src1", ambiguousName);
                var src2ProjectPath = Path.Combine(solutionPath, "src2", ambiguousName);
                var expectedMessage = $@"'{ambiguousName}' is an ambiguous name resolved to following projects:
{src1ProjectPath}
{src2ProjectPath}";

                Project project = null;
                var resolver1 = new ProjectResolver(src1ProjectPath);
                var exception = Assert.Throws<InvalidOperationException>(() => resolver1.TryResolveProject(ambiguousName, out project));
                Assert.Contains(expectedMessage, exception.Message);
                Assert.Null(project);

                var resolver2 = new ProjectResolver(src2ProjectPath);
                exception = Assert.Throws<InvalidOperationException>(() => resolver2.TryResolveProject(ambiguousName, out project));
                Assert.Contains(expectedMessage, exception.Message);
                Assert.Null(project);
            }
        }

        [Fact]
        public void ProjectResolverDoesNotThrowWhenAmbiguousNameIsNotUsed()
        {
            const string ambiguousName = "ProjectA";
            const string unambiguousName = "ProjectB";
            var solutionStructure = @"{
  'global.json': '',
  'src1': {
    'ProjectA': {
      'project.json': '{}'
    },
    'ProjectB': {
      'project.json': '{}'
    }
  },
  'src2': {
    'ProjectA': {
      'project.json': '{}'
    }
  }
}";

            using (var solutionPath = new DisposableDir())
            {
                DirTree.CreateFromJson(solutionStructure)
                    .WithFileContents("global.json", @"{
  ""projects"": [""src1"", ""src2""]
}")
                    .WriteTo(solutionPath);

                var ambiguousProjectPath = Path.Combine(solutionPath, "src1", ambiguousName);
                var unambiguousProjectPath = Path.Combine(solutionPath, "src1", unambiguousName);

                Project project;
                Assert.True(new ProjectResolver(ambiguousProjectPath).TryResolveProject(unambiguousName, out project));
                Assert.NotNull(project);

                project = null;
                Assert.True(new ProjectResolver(unambiguousProjectPath).TryResolveProject(unambiguousName, out project));
                Assert.NotNull(project);
            }
        }
    }
}
