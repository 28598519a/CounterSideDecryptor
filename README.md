# CounterSideDecryptor
用於解密 未來戰 (Use to decrypt Counter:Side *.asset file)[2023ver.]

本工具僅適用較近期的版本，花了比預想的還要更多的時間，il2cpp真是討厭的東西... (一堆C++混著C#的類，特別難搞)

## Usage
把所有檔案放進exe目錄下的CounterSide目錄中，執行
![](https://user-images.githubusercontent.com/33422418/215161524-ef317b01-1fdc-4fae-815c-967faf1dff12.png)

## Known issue
1. Some file extensions were not properly included, such as *.gbl *.eng ...etc
2. Some files in Assetbundle or StreamingAsset don't have file extension, but it is Unity asset file.

Because the decryption are releated to completed file name. Therefore, it is impossible to decrypt by manually adding the file extension or modifying the file extension to force CounterSideDecryptor identifies and completes the decryption process.

Manually workaround is delete `.Where(f => extensions.Contains(f.Extension.ToLower())`. However, you must ensure that the files to be processed are all encrypted Unity assets. Otherwise, it may cause damage to the unencrypted files.

Welcome to submit pull requests to address these issues.

## 參考了這篇 (早期版本):
https://blog.palug.cn/2966.html
