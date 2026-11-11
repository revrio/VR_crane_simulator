mergeInto(LibraryManager.library, {
  PortLabDownload: function(pointer, length, filename) {
    var blob = new Blob([HEAPU8.slice(pointer, pointer + length)], {type:'application/zip'});
    var url = URL.createObjectURL(blob);
    var link = document.createElement('a');
    link.href = url; link.download = UTF8ToString(filename);
    document.body.appendChild(link); link.click(); link.remove();
    setTimeout(function() { URL.revokeObjectURL(url); }, 60000);
  }
});
