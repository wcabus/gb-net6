// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const drawContext = document.getElementById('viewport').getContext('2d');

setModuleImports('main.js', {
    outputImage: (pixels, width, height) => {
        var arr = new Uint8ClampedArray(pixels.length);
        arr.set(pixels, 0);

        var imageData = new ImageData(arr, width, height, {});
        createImageBitmap(imageData).then(image => {
            drawContext.drawImage(image, 0, 0, width * 4, height * 4);
        });
    }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

window.addEventListener('keydown', (e) => {
    if (e.isComposing || e.keyCode === 229) {
        return;
    }

    exports.Interop.KeyDown(e.code);
});

window.addEventListener('keyup', (e) => {
    if (e.isComposing || e.keyCode === 229) {
        return;
    }

    exports.Interop.KeyUp(e.code);
});

await runMain();