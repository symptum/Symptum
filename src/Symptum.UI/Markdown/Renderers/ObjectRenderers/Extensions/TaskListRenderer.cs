// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Extensions.TaskLists;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Extensions;

internal class TaskListRenderer : WinUIObjectRenderer<TaskList>
{
    protected override void Write(WinUIRenderer renderer, TaskList taskList)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(taskList);

        MyTaskListCheckBox checkBox = new(taskList);
        renderer.WriteInline(checkBox);
    }
}
