# Debug と Release の日付を比較
set bindir WindowsFormsApp1/bin
set debug_mtime   [file mtime  $bindir/Debug/DefenceAligner.exe]
set release_mtime [file mtime  $bindir/Release/DefenceAligner.exe]
if {$debug_mtime > $release_mtime} {
    puts stderr {Release binary is older than the Debug binary. Please re-build the Release binary.}
    exit 0
}

set outdir DefenceAllocator-$release_mtime
if {[file isdirectory $outdir]} {
    file delete -force $outdir
}
file mkdir $outdir
exec pandoc -f gfm -t html README.md > $outdir/README.html
foreach img [glob *.png] {
    file copy $img $outdir/$img
}
foreach xls [glob *.xlsx] {
    file copy $xls $outdir/$xls
}
file copy $bindir/Release $outdir
set f [open $outdir/DefenceAllocator.bat w]
puts $f {@echo off
if not "%~0"=="%~dp0.\%~nx0" (
     start /min cmd /c,"%~dp0.\%~nx0" %*
     exit
)
start Release\DefenceAligner.exe
}
close $f
exec zip -r $outdir.zip $outdir
file delete -force $outdir
