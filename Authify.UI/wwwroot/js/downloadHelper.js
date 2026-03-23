export function downloadFileFromBase64(filename, base64Content) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/json;base64," + base64Content;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};