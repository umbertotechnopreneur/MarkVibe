async function script(path) {
  await new Promise((resolve, reject) => {
    const element = document.createElement("script"); element.src = new URL(path, document.baseURI);
    element.onload = resolve; element.onerror = reject; document.body.append(element);
  });
}
try {
  await import("./js/markvibe.js");
  await script("libs/OpenSilver.js");
  await script("_framework/blazor.webassembly.js");
} catch {
  const message = document.getElementById("boot-message");
  if (message) message.textContent = "The viewer could not load. Check its local assets and reload this page.";
  document.getElementById("blazor-error-ui").style.display = "block";
}
