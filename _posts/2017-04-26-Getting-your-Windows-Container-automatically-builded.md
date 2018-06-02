---
layout: post
title:  "Getting your Windows Container automatically builded"
date:  2017-04-26
tags: [WindowsContainers, Docker, AppVeyor, RavenCage]
---

I have a pet project called [RavenCage](github.com/pizycki/RavenCage). It's main goal is to containerize [RavenDB](https://ravendb.net) on [Docker](https://www.docker.com). Its Docker images are available on [DockerHub](). 

This is how I managed to automate my most of my work in publishing images with new releases with [AppVeyor](http://appveyor.com).

## First: Containerize!

RavenDB is document database. One of its greatest advantages is its lightness. DB Engine with Web UI is packed in the ZIP file which weights about 50 MB. It's standalone executable program, so you don't need to install anything fancy except .NET Framework.

Oh yeah, I mentioned .NET Framework. Noo, not .NET Core.

RavenDB 3.5.3* works on .NET 4.5, so we need [Windows NT kernel](https://en.wikipedia.org/wiki/Windows_NT). Fortunately there was Microsoft container engine implementation with compatible [Docker API](https://docs.docker.com/engine/api/) called [Windows Containers](https://docs.microsoft.com/en-us/virtualization/windowscontainers/about/) that works on Windows NT. It is available from Windows 10 and Windows Server 2016.

I recommend installing Windows Server 2016 locally on Virtual Machine. After installation, update your system. Then install Windows Containers and Docker client.

```powershell
#Enable PowerShell scripts
Set-ExecutionPolicy remotesigned

#Install Windows Containers
Install-WindowsFeature containers
Restart-Computer -Force
```

The machine will restart itself after this.

```powershell
#Install and start Docker
Invoke-WebRequest "https://download.docker.com/components/engine/windows-server/cs-1.12/docker-1.12.2.zip" -OutFile "$env:TEMP\docker.zip" -UseBasicParsing
Expand-Archive -Path "$env:TEMP\docker.zip" -DestinationPath $env:ProgramFiles
#For quick use, does not require shell to be restarted.
$env:path += ";c:\program files\docker"
#For persistent use, will apply even after a reboot. 
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\Program Files\Docker", [EnvironmentVariableTarget]::Machine)
dockerd.exe --register-service
Start-Service docker
```

Now you should be able to call Docker API. Type `docker info` to see if everything is alright.

## Build your images

Once we're up with our workspace we can create our Docker image.

Here is Dockerfile which produces RavenDB 3.5 Docker image.

> Actual version is available [here](https://github.com/pizycki/RavenCage-3.5/blob/master/Dockerfile).

```dockerfile
FROM microsoft/windowsservercore
MAINTAINER pizycki

EXPOSE 8080

# Default mode server runs.
ENV mode=background

# Download RavenDB server package from official build website
ADD http://hibernatingrhinos.com/downloads/ravendb/35194-Patch C:/RavenDB_Server.zip
COPY Run-RavenDB.ps1 .
CMD powershell ./Run-RavenDB.ps1 -Verbose
```

As you can see, there is downloaded specific version of RavenDB during image build. 

Every time new version came out, I had to change the version inside Dockerfile (usually it's a single digit) and build/tag/test/publish new version of image. After performing this operations for few times, completing the whole process took me less then 30 mins. Not that bad, but indeed, **it was no fun**.

So I've started looking for way to automate this process.

## Manually automated

Firstly, I've created powershell script that did everything for me by itself.

```powershell
# Creates new version of Docker image with specified tag.

Param(
  [Parameter(Mandatory=$True)]
  [string]$Tag = "",
  [switch]$Latest,
  [string]$Repository = "pizycki/ravendb",
  [switch]$PushToDockerHub,
  [string]$CredentialsPath = "credentials",
  [string]$DockerHubUser = "",
  [string]$DockerHubPass = "",
  [switch]$DontSignOut
)

#### Functions ####

function Create-ImageInRegistry([string]$repository, [string]$tag) {
    
    # Build new Docker image
    write "Building image."
    try {
        docker build --no-cache -t ${repository}:${tag} .
    }
    catch [System.Exception] {
        throw "Error during image build. (${repository}:${tag})"
    }
    write "Image builded!"

    # Check if images is in registery
    write "Checking image in registery..."
    $image_in_registery = (docker images `
                                | sls $repository `
                                | sls $tag).count -gt 0

    if (!($image_in_registery)) {
        throw "Image not found in registery after build. (${repository}:${tag})"
    }
    write "Image found in registery!"
}


function Push-ImageToDockerHub(
    [string]$repository,
    [string]$tag,
    [string]$user,
    [string]$password)
{
    write "Logging in to Docker Hub as [ $user ] ..."
    docker login -u $user -p $password

    if ( $? ) {
        write "Pushing [ ${repository}:${tag} ] to Docker Hub..."
        docker push ${repository}:${tag}
        write "Image pushed!"        
    }
}

function TagAsLatest([string]$repository){
    write "Tagging image [ ${repository}:${tag} ] as latest..."
    docker tag ${repository}:${tag} ${repository}:latest
    write "Tagged as 'latest' !"
}

#### Main ####

# Validate params
if ( [System.String]::IsNullOrWhiteSpace($tag) ) { throw "Tag cannot be null or empty." }
if ( [System.String]::IsNullOrWhiteSpace($repository) ) { throw "Tag cannot be null or empty." }

#### ENV ####

# Start docker if it's not running
try { start docker }
catch [System.InvalidOperationException] { throw "Docker is not in PATH" }

# Check presence of Dockerfile and dockerignore
if ( !(Test-Path ".\Dockerfile") ) { throw "Missing Dockerfile. Are you sure it's Docker repository root?" }
if ( !(Test-Path ".\.dockerignore") ) { throw "Missing dockerignore file" }


#### BUILD ####

# Create image and get image ID
Create-ImageInRegistry $Repository $Tag

# Tag as latest
if ( $Latest ) {
    TagAsLatest $Repository $Tag
}

# DockerHub publish
if ( $PushToDockerHub ) {

    #### Docker Hub credentials ####
    if (Test-Path $CredentialsPath) {

        # Load credentials from file
        $file = Get-Content credentials
        $DockerHubUser = $file[0]
        $DockerHubPass = $file[1]
    }

    # Validate credentials
    if ( [System.String]::IsNullOrWhiteSpace($DockerHubUser) -or `
         [System.String]::IsNullOrWhiteSpace($DockerHubUser) ) {
            throw "Not valid credentials"
    }

    # Publish tagged image to DockerHub
    Push-ImageToDockerHub $Repository $Tag $DockerHubUser $DockerHubPass

    if ( $Latest ) {
        Push-ImageToDockerHub $Repository "latest" $DockerHubUser $DockerHubPass
    }

    if ( !$DontSignOut ) {
        # Removes temporary file that holds user credentials
        # Ref: https://docs.docker.com/engine/reference/commandline/login/
        docker logout
    }
}
```

Building, tagging, publishing... All I had to do was to run single powershell command in my... VM.

Yes, sadly, I still needed my VM. The VM that I keep only on my home PC.

That effectively narrowed down my places where I could build the image and publish it to the DockerHub.

It was time to find a build platform.

## To the cloud !

[DockerHub](https://hub.docker.com) allows you to synchronize to [GitHub](https://github.com) and build Dockerfile found inside your Git repository. Cool, but works only with linux-based images. The same thing with [Travis](https://travis-ci.org). 

It was possible to host your own Continous Integration on [Visual Studio Team Services](https://marketplace.visualstudio.com/items?itemName=ms-vscs-rm.docker), but it involved charges. Besides that, I think it's a bit too complicated. I simply didn't like it.

A month ago, [AppVeyor](https://www.appveyor.com), the true hero of OpenSource and CI/CD of .NET, finally released Virtual Machine Images with pre-installed Visual Studio 2017 and Windows Containers (HyperV). **This was it.**

I've created build script basing on the script that I used to build my images manually, but it went out pretty mess, so I refactored it and split it into seperate files.

The `Build-Image.ps1` is called in "Build" phase. It builds Dockerfile and tags it with Git tag and as `latest`.
```powershell
# Build-Image.ps1

Import-Module .\Common-Module.psm1 3>$null
Import-Module .\Docker-Module.psm1 3>$null

Write-Frame "Building Dockerfile" Magenta

[string]  $repository = Get-EnvVariable "DOCKER_REPOSITORY"
[boolean] $tagged     = [System.Convert]::ToBoolean((Get-EnvVariable "APPVEYOR_REPO_TAG"))
[string]  $tag        = Get-EnvVariable "APPVEYOR_REPO_TAG_NAME"
[boolean] $latest     = [System.Convert]::ToBoolean((Get-EnvVariable "IS_LATEST"))

Write-Host "Check if commit is tagged, if no, break the build."
# Set in AppVeyor flag: Build tags only.
Get-EnvVariable "APPVEYOR_REPO_TAG"

Write-Host -ForegroundColor Green "All looks good! Continue with build."

Write-Host "Build image from Dockerfile."
Create-Image $repository $tag

# Set image as 'latest' according to build settings.
if ( $latest ) {
    Tag-AsLatest $repository $tag
}
```

`Test-Container` script tests running container of just builded image. If any test failure brokes the build.
```powershell
# Test-Container.ps1

Import-Module .\Common-Module.psm1   3>$null
Import-Module .\Security-Module.psm1 3>$null

Write-Frame "Testing: This script will perform bunch of simple scripts making sure that RavenDB can be run and is accessible." Magenta

[string] $repository = Get-EnvVariable "DOCKER_REPOSITORY"
[string] $tag        = Get-EnvVariable "APPVEYOR_REPO_TAG_NAME"
[string] $name       = "testo"
[int]    $bindPort   = 8080

Write-Host "Enabling port ${bindPort}. Is that ok?"
netsh advfirewall firewall add rule name="Open Port 8080" dir=in action=allow protocol=TCP localport=${bindPort}

Write-Host "Disabling some Windows security features (for testing)."
Disable-UserAccessControl
Disable-InternetExplorerESC
Write-Host "Running '${name}' container."
Write-Host "Container ID will be written below."
docker run -d --name $name -p ${bindPort}:8080 ${repository}:${tag}

Write-Host "Making sure container has started. Docker FAQ says its usualy 10 secs so let's assume that."
Start-Sleep -Seconds 10
Write-Host "Done waiting, proceeding to tests."

Write-Host "Checking container is up."
if ( (docker ps | sls $name).Length -eq 0 ) { Exit-WithError "Test FAILED: No running container with name '${$name}'." }
Write-Success "Container is up and running!"

$ip = docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $name
$uri = "http://${ip}:${bindPort}/"
Write-Host "RavenStudio should be hosted on ${uri}"
Write-Host "Sending request to RavenStudio..."
$response = Invoke-WebRequest -Uri $uri
if ( $LastExitCode -ne 0 ) { Exit-WithError "Error while requesting Raven Studio." }    
if ( $response.StatusCode -ne 200 ) { Exit-WithError "Test FAILED: Got non-200 HTTP code." }
Write-Success "Connected to Raven Studio!"

Write-Success "All tests passed." -frame
```

With `Publish-Image` script we log in to Docker session and push builded images.
```powershell
# Publish-Image.ps1

Import-Module .\Common-Module.psm1 3>$null
Import-Module .\Docker-Module.psm1 3>$null

[boolean] $pushToDockerHub   = [System.Convert]::ToBoolean((Get-EnvVariable "PUSH_TO_DOCKERHUB"))
[string]  $repository        = Get-EnvVariable "DOCKER_REPOSITORY"
[string]  $tag               = Get-EnvVariable "APPVEYOR_REPO_TAG_NAME"
[boolean] $latest            = [System.Convert]::ToBoolean((Get-EnvVariable "IS_LATEST"))
[string]  $dockerHubUser     = Get-EnvVariable "DOCKERHUB_USER"
[string]  $dockerHubPassword = Get-EnvVariable "DOCKERHUB_PASSWORD"

if ( $pushToDockerHub ) {

    Write-Frame "Publishing image to DockerHub" Magenta

    Write-Host "Publishing '${repository}:${tag}'." 
    Push-ImageToDockerHub $repository $tag $dockerHubUser $dockerHubPassword
    Write-Success "Image '${repository}:${tag}' has been published!"

    Write-Host "Is latest? ${latest}"
    if ( $latest ) {
        Write-Host "Publishing '${repository}:${tag}' as 'latest'" 
        Push-ImageToDockerHub $repository "latest" $dockerHubUser $dockerHubPassword
        Write-Success "Image '${repository}:latest' has been pushed to DockerHub!"
    }

    # Removes temporary file that holds user credentials
    # Ref: https://docs.docker.com/engine/reference/commandline/login/
    docker logout
}
```

The powershell modules are for better readability.
```powershell
# Docker-Module.psm1
function Create-Image( [string] $repository, [string] $tag ) {
    
    write "Building image."

    docker build --no-cache -t ${repository}:${tag} .
    
    if ( $LastExitCode -ne 0 ) { Exit-WithError "Error during image build. (${repository}:${tag})" }
    
    write "Image builded!"

    # Check if images is in registery
    write "Checking image in local registery..."
    $image_in_registery = (docker images `
                          | sls $repository `
                          | sls $tag).count -gt 0

    if ( $image_in_registery -eq $false ) { Exit-WithError "Image not found in registery after build. (${repository}:${tag})" }
    if ( $LastExitCode -ne 0 ) { Exit-WithError "Error during image look up" }

    write "Image found in registery!"
}

function Push-ImageToDockerHub(
         [string]$repository,
         [string]$tag,
         [string]$user,
         [string]$password) {
    
    write "Logging in to Docker Hub as [ $user ] ..."
    docker login -u $user -p $password

    if ( $? ) {
        write "Pushing [ ${repository}:${tag} ] to Docker Hub..."
        docker push ${repository}:${tag}
        if ( $LastExitCode -ne 0 ) { Exit-WithError "Error during pushin image to DockerHub." }
        write "Image pushed!"        
    }
}

function Tag-AsLatest( [string]$repository, [string] $tag ){
    write "Tagging image [ ${repository}:${tag} ] as latest..."
    docker tag ${repository}:${tag} ${repository}:latest
    if ( $LastExitCode -ne 0 ) { Exit-WithError "Error during tagging image as 'latest'." }
    write "Tagged as 'latest' !"
}
```

```powershell
# Common-Module.psm1
function Exit-WithError( $message ){
    $errorExitCode = 1
    Write-Error $message
    $host.SetShouldExit( $errorExitCode )
    exit
}

function Get-EnvVariable( $name ){
    $value = (Get-Item env:$name).Value
    if ( $value -eq $null ) {
        Exit-WithError "Env variable is not set."
    }
    return $value
}

function Write-Frame( [string] $message, [string] $foregroundColor = "White" ) {
    Write-Host -ForegroundColor $foregroundColor "***********************************"
    Write-Host -ForegroundColor $foregroundColor $message
    Write-Host -ForegroundColor $foregroundColor "***********************************"
}

function Write-Success( [string] $message, [switch] $frame ) {    
    if ( $frame ) {
        Write-Frame $message green
    } else {
        Write-Host -ForegroundColor Green $message
    }
}
```

Those  files are located in root of my Git project.

> If you have questions about the scripts, please leave a comment or tweet me!

In addition there is one more file called `appveyor.yml`. This is a build configuration file, exported through AppVeyor. When AppVeyor detects a new commit in your Git repo, it gets this file as base for your build. If you want to override this build, you can do it on AppVeyor portal. Both `yml` file and portal offer the same functionality.

```yaml
version: 1.0.{build}
skip_non_tags: true
image: Visual Studio 2017
clone_depth: 1
init:
- cmd: docker info & docker ps
environment:
  DOCKER_REPOSITORY: pizycki/ravendb
  DOCKERHUB_USER: pizycki
  DOCKERHUB_PASSWORD:
    secure: ygOvGB...w3E=
  IS_LATEST: true
  PUSH_TO_DOCKERHUB: true
build_script:
- ps: '& .\Build-Image.ps1'
test_script:
- ps: '& .\Test-Container.ps1'
deploy_script:
- ps: '& .\Publish-Image.ps1'
```

Builds are triggered only on tagged commits. The Git tag is retrieved and used to tag Docker image. If flag `IS_LATEST` is up, the image tagged as latest is also published to DockerHub.



## This is how we roll

With this build setup we can no longer worry about building, testing and publishing our images.

But how to trigger the build?

I mentioned that builds are triggered only on tagged commits. This would require us to:

* **clone** repo (`git clone http://...`)
* do **change**s (in `Dockerfile`)
* **commit** changes (`git add -A & git commit -m "..."`)
* **tag** commit (`git tag ...`)
* and make a **push** to the server (`git push`)

Unless you have GitHub.



With GitHub you can [edit any text file](https://help.github.com/articles/editing-files-in-your-repository/) and [create tags](https://help.github.com/articles/creating-releases/) **online!** 

So when I get notification about new version of RavenDB, all I have to do is:

* open my repository on GitHub
* change version of RavenDB in `Dockerfile`
* save it
* create new release (with tag the same as RavenDB version)

In couple of minutes I should receive an email notification with status of my build.

## Super-duper badge

With automation of image building I reduced my work in this project to minimum. It's reliable. It's stable. 

And I can finally put this super-cool badge in `Readme` file.

[![Build status](https://ci.appveyor.com/api/projects/status/ab7oryewihivh46x?svg=true)](https://ci.appveyor.com/project/pizycki/ravencage-3-5)

Yes, that was the main reason to do the whole thing.

Yay.

# Update:
Not so long after publish of this post, [RavenDB officialy started publish their own Docker images](https://ayende.com/blog/178049/ravendb-4-0-on-docker). Fortunetly, I can still publish images for RavenDB 3.5 as the Raven Team is focusing on RavenDB 4.0 and are not interested in supporting currently stable release.



As long as RavenDB 3.5 releases will be issued, I will continue publishing their Docker images.