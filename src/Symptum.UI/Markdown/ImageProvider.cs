// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Symptum.UI.Markdown;

public interface IImageProvider
{
    Task<Image> GetImage(string url);

    bool ShouldUseThisProvider(string url);
}
