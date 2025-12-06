$dllPath="E:\H3VR\H3VR-Supply-Raid\Packer-SupplyRaid\plugin\bin\Debug\net35\SupplyRaid.dll"
$mdbPath="E:\H3VR\H3VR-Supply-Raid\Packer-SupplyRaid\plugin\bin\Debug\net35\SupplyRaid.dll.mdb"

# 都复制到
$dst = "C:\Users\zec\AppData\Roaming\Thunderstore Mod Manager\DataFolder\H3VR\profiles\safehouse\BepInEx\plugins\Packer-SupplyRaid"
Copy-Item $dllPath $dst -Force
Copy-Item $mdbPath $dst -Force
echo "Copied $dllPath and $mdbPath to $dst"
echo "Done."