# RuleFlow-RenderEngine
Provides a rendering engine and networking protocol for streaming events from RuleFlow Studio. It generally follows the hub-spoke principles of RuleFlow.

Build from src to create optimized export templates:
Make the "slimmed_export.gdbuild" config using Godot Editor -> Tools -> Engine Compilation Configuration Editor.
`uv tool install scons` on the main windows system.
Make sure to add `C:\msys64\ucrt64\bin` to PATH (after installing MinGW with msys2).
`scons platform=windows tools=no target=template_release optimize=size production=yes lto=none d3d12=yes angle=no use_mingw=yes build_profile="../RuleFlow-RenderEngine/slimmed_export.gdbuild" -j8`
Run `strip path/to/godot.binary` to clean up strip debug symbols.

- [docs.godotengine.org/en/stable/engine_details/development/compiling/introduction_to_the_buildsystem.html#](https://docs.godotengine.org/en/stable/engine_details/development/compiling/introduction_to_the_buildsystem.html#)
- [popcar.bearblog.dev/how-to-minify-godots-build-size/](https://popcar.bearblog.dev/how-to-minify-godots-build-size/)
- [docs.godotengine.org/en/stable/tutorials/export/exporting_projects.html](https://docs.godotengine.org/en/stable/tutorials/export/exporting_projects.html)
- [docs.godotengine.org/en/stable/engine_details/development/compiling/optimizing_for_size.html#doc-optimizing-for-size](https://docs.godotengine.org/en/stable/engine_details/development/compiling/optimizing_for_size.html#doc-optimizing-for-size)
- [docs.godotengine.org/en/4.4/contributing/development/compiling/getting_source.html](https://docs.godotengine.org/en/4.4/contributing/development/compiling/getting_source.html)
