$framework = "4.0"

properties {
	$projectName = "CssDiff"
	$version = "0.1.0.1"
	$projectConfig = "Release"
	
	$company = $projectName
	$base_dir = resolve-path ..
	$build_dir = resolve-path .
	$src_dir = "$base_dir\source"
	$sln = "$src_dir\$projectName.sln"
	$package_dir = "$base_dir\dist"
	$test_dir = "$base_dir\test"
	$msbuild_output = "$src_dir\cssdiff\bin\$projectConfig"
}

task default -depends Merge

task Test -depends Compile {

	exec {
		
		# i know this is a brittle test.. but I just want to do something end-to-end to see the thing work
		delete_directory $test_dir
		create_directory $test_dir
		copy_files $msbuild_output $test_dir

		# run the exe
		$actual = & "$test_dir\$projectName.exe" -f $test_dir\TestFrom.css -t $test_dir\TestTo.css -v=quiet
		
		# get expected output
		$expected = gc "$build_dir\expected-test-output"
		# assertion
		if (diff $actual $expected) {
			diff $actual $expected -includeequal | write-host  -ForeGroundColor 'RED'
		}
	
	}
}

task Compile -depends Clean, CommonAssemblyInfo { 
	exec { msbuild /t:build /v:m /nologo /p:Configuration=$projectConfig $sln }
}

task CommonAssemblyInfo {
	create-commonAssemblyInfo "$src_dir\AssemblyInfo.cs"
}

task Clean { 
	msbuild /t:clean $sln
}

task ResetPackage {
	delete_directory $package_dir
	create_directory $package_dir
}

task Package -depends Compile, ResetPackage {
	copy_files $msbuild_output $package_dir "*.css"
}

task Merge -depends Compile, ResetPackage {
	$temp = "$build_dir\temp_merge"
	create_directory $temp
	
	& "$build_dir\ILMerge" /out:$temp/cssdiff.exe /v4 $msbuild_output/CssDiff.exe $msbuild_output\BoneSoft.Css.dll	
	
	Get-ChildItem "$temp\**" -Include *.exe | Copy-Item -Destination $package_dir
	delete_directory $temp
}

task ? -Description "Helper to display task info" {
	Write-Documentation
}

###

function global:zip_directory($directory,$file) {
    write-host ""Zipping folder: "" $test_assembly
    delete_file $file
    cd $directory
    & ""$base_dir\lib\7zip\7za.exe"" a -mx=9 -r $file
    cd $base_dir
}


function global:copy_files($source,$destination,$exclude=@()){    
    create_directory $destination
    Get-ChildItem $source -Recurse -Exclude $exclude | Copy-Item -Destination {Join-Path $destination $_.FullName.Substring($source.length)} 
}

function global:Copy_and_flatten ($source,$filter,$dest) {
  ls $source -filter $filter -r | cp -dest $dest
}

function global:copy_all_assemblies_for_test($destination){
  create_directory $destination
  Copy_and_flatten $source_dir *.exe $destination
  Copy_and_flatten $source_dir *.dll $destination
  Copy_and_flatten $source_dir *.config $destination
  Copy_and_flatten $source_dir *.xml $destination
  Copy_and_flatten $source_dir *.pdb $destination
  Copy_and_flatten $source_dir *.sql $destination
  Copy_and_flatten $source_dir *.xlsx $destination
}

function global:delete_file($file) {
    if($file) { remove-item $file -force -ErrorAction SilentlyContinue | out-null } 
}

function global:delete_directory($directory_name)
{
  rd $directory_name -recurse -force  -ErrorAction SilentlyContinue | out-null
}

function global:delete_files_in_dir($dir)
{
	get-childitem $dir -recurse | foreach ($_) {remove-item $_.fullname}
}

function global:create_directory($directory_name)
{
  mkdir $directory_name  -ErrorAction SilentlyContinue  | out-null
}

function global:create-commonAssemblyInfo($filename)
{
"using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle(""$projectName"")]
[assembly: AssemblyDescription("""")]
[assembly: AssemblyConfiguration(""$projectConfig"")]
[assembly: AssemblyCompany(""$company"")]
[assembly: AssemblyProduct(""$projectName"")]
[assembly: AssemblyCopyright(""Copyright © $company 2011"")]
[assembly: AssemblyTrademark("""")]
[assembly: AssemblyCulture("""")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion(""$version"")]
[assembly: AssemblyFileVersion(""$version"")]"  | out-file $filename -encoding "UTF8"    
}