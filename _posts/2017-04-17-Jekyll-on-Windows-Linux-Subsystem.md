---
layout: post
title:  "Jekyll on Windows Linux Subsystem"
date:  2017-04-17
categories: [Jekyll, WSL]
---

[This](http://www.izzydev.net) blog is generated with [Jekyll static site generator](https://jekyllrb.com). In comparison to all Wordpress-like blogs, there is no backend logic. No content management. No databases. Whole website is bunch of pregenerated HTML files, so no matter how crappy your hosting is, your site will always run fast.

Unfortunately to run Jekyll we need Ruby. Don't get me wrong, there is nothing wrong with Ruby itself. But the work with Ruby on Windows is just... #@!%#@$. It's much more simple on Linux or Mac. 

You still can still use Jekyll on Windows. [But since Jekyll is a lot more popular on UNIX systems, theme creators don't really care about Windows so much.](https://www.quora.com/Is-it-a-bad-idea-to-use-Ruby-on-Rails-on-Windows/answer/Zachary-Weiner-5?srid=DCfO) Sometime you cannot build your favorite theme because of lack of libraries available only on unix-es. Just like the [theme](https://github.com/agusmakmun/agusmakmun.github.io) that I've choosed which is based on [Nokogiri](http://www.nokogiri.org).

What can be done in this situation? 

## Choose other theme
No! I've spent many hours looking for theme nice and simple, yet practical (tags, search function). This theme fits most of my needs. Change is not an option.
## Create linux VM and work there
This is how I worked with my blog for a quite while. At any time I could spin up my VM and start writing. That was quite comfortable. **But I like keep my software updated.** Every time restored my VM there were updates available: Ubuntu updates, VS Code updates, VS Code plugins updates, Typora updates, and so on and so on.

Another thing are high requirements to run VM. You need RAM and fast disk. 

Copying VM instances to your all workstations is not the best idea too.

## Docker with Toolbox
You can install [Docker Toolbox](https://www.docker.com/products/docker-toolbox) which allows you to run Docker containers with Linux-based images. Unfortunately to enable Docker Toolbox you have to turn on HyperV support. If you don't own Windows in Pro edition or you prefer using other VM software, like VMware (me) or VirtualBox, you won't be happy with that restriction.

In case you were instersted in running Jekyll in conatiner, here is one I used for my blog.

```dockerfile
FROM ubuntu:xenial
MAINTAINER pizycki

EXPOSE 4000 35729

RUN mkdir ~/izzydev
WORKDIR ~/izzydev

RUN \
    apt-get update && \
    apt-get install \
		ruby \
		git \
		nodejs \
		build-essential \
		patch \
		ruby-dev \
		zlib1g-dev \
		liblzma-dev -y && \
    gem install \	
	jekyll \
	bundle && \
    git clone https://github.com/agusmakmun/agusmakmun.github.io . && \
    bundle update && \
    bundle install

CMD bundle exec jekyll serve
```



## Windows Linux Subsystem (WSL) a.k.a. Bash on Ubuntu on Windows

[WSL](https://msdn.microsoft.com/en-us/commandline/wsl/about) is new feature introduced in [Windows 10 Anniversary Update](http://www.windowscentral.com/how-get-windows-10-anniversary-update). In short, it's Ubuntu working on Windows kernel. It doesn't require HyperV (yupi!) and doesn't affect your PC performance. The only requirements are Win 10 with AE udpate and 64 bit OS.

What's so cool about it? You can run Jekyll generator in WSL (Linux) and edit your site on Windows. **That's exactly what we were looking for.**

Downsides? WSL is still in beta and Jekyll 'watch' task doesn't work because [some implementation is missing](https://github.com/Microsoft/BashOnWindows/issues/216). And, if you're new to Linux OSes, you may need to learn some new things like 'bash' and linux environment, (which I highly recommend to do anyway).

### Installation WSL

Installation comes to [few clicks](https://msdn.microsoft.com/en-us/commandline/wsl/install_guide).

If you're console-guy, just invoke this powershell script

```powershell
# Enable Windows Developer mode
# Copied from http://stackoverflow.com/a/40033638/864968

# Create AppModelUnlock if it doesn't exist, required for enabling Developer Mode
$RegistryKeyPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"
if (-not(Test-Path -Path $RegistryKeyPath)) {
    New-Item -Path $RegistryKeyPath -ItemType Directory -Force
}
 
# Add registry value to enable Developer Mode
New-ItemProperty -Path $RegistryKeyPath -Name AllowDevelopmentWithoutDevLicense -PropertyType DWORD -Value 1
 
 
# Enable WSL
# Copied from http://www.thomasmaurer.ch/2016/11/how-to-install-linux-bash-on-windows-10/
Enable-WindowsOptionalFeature -Online -NoRestart -FeatureName Microsoft-Windows-Subsystem-Linux
```

### Installing Jekyll

To enter WSL simply type `bash` in your CLI (like cmd or [cmder]()).

To setup env for my blog development I use script listed below. I can download it with `curl` and invoke by single line.

```bash
# Get&Run: 
# curl < URL > -o ~/install-jekyll.sh && bash ~/install-jekyll.sh

# Print Ubuntu version
lsb_release -a

# Upgrade Ubuntu to latest
sudo do-release-upgrade

# Install software
sudo apt-add-repository ppa:brightbox/ruby-ng
sudo apt-get update
sudo apt-get install -y ruby2.3 git nodejs build-essential patch ruby2.3-dev zlib1g-dev liblzma-dev
sudo gem install jekyll bundler

# Clone project
mkdir ~/dev
git clone https://github.com/pizycki/pizycki.github.io ~/dev/izzydev
cd ~/dev/izzydev

# Build and run
bundle install
bundle exec jekyll serve --no-watch
```

To run Jekyll we need ruby at least at version 2.0. [You can install it on different ways](https://gorails.com/setup/windows/10), but this one worked for me without any bigger issues. Just add 'apt' repository and install `ruby2.3`. Installing [rvm](https://rvm.io), [rbenv](https://github.com/rbenv/rbenv) or even compiling from source, always ended with some missing library.

For Hanselman [it worked](https://www.hanselman.com/blog/RubyOnRailsOnAzureAppServiceWebSitesWithLinuxAndUbuntuOnWindows10.aspx) with installing rbenv. You may tried it as well.

Unfortunately the whole installation process is not fully automatic, we still need to perform some input during it (mainly providing root password).

### Develop

The script will start micro web server which will listen for requests on `http://localhost:4000`. Unlike with Windows Containers, there are no localhost binding issues. **You won't even feel that you run your server on Ubuntu.**

After setting up blog theme, it comes to writing posts. For this task I use [Typora](https://www.typora.io), markdown editor based on [Electron engine](https://electron.atom.io). Sure, I can use VS Code or any other text editor with suitable plugin, but Typora is just meant for this job and ineed it does it right.

#### Line endings

There is nothing keeping you from accessing your data on both Win and Linux at the same time. The only thing you may care about is consistent [line ending style](http://www.cs.toronto.edu/~krueger/csc209h/tut/line-endings.html). Most of modern text editors and IDEs have allow to configure it. For example, [here is how you can do it in VS Code](http://stackoverflow.com/a/42643643/864968). Edit your `User.Settings`

```json
"files.eol": "\n"
```

There is also an icon (`CRLF` and `LF`) indicating line ending format for current file in right bottom corner of the window.

#### Keeping your data in sync

WSL automatically attach your Windows disk partitions. You can browse them navigating to `/mnt` directory. In fact, it is highly recommended to place your projects on those partitions. Otherwise, you may loose your data after quitting bash session.

I keep my blog on Dropbox and Git. In that way, I don't have to fetch/pull and commit/push every time I change a computer. Dropbox keep my blog the same state on every computer. As far as there is only one person that commit to Git, it's fine to keep repo on storage cloud. In fact, Git repo are just files.

To host my blog I use [GitHub Pages](https://pages.github.com). Whenever I want to publish a new post all I have to do is pushing all my changes to `origin/master`. That's it. Publishing with single command line. This is what I really like about that approach.

### Reinstalling WSL

In case you mess up something you can reinstall fresh instance with two commands. Remember to run them **separately**.

```bash
lxrun /uninstall /full
lxrun /install
```

## Is it worth all this effort?	

Okay, we have setup WSL with our blog. Was that worth all that time we have spent?

WSL has huge potential. It's a bridge from Windows to Linux without VMs. You might disagree with me, but I think there is something in unix based OSes that makes them great, something that Windows lacks. Installing Bash on Windows allow getting familiar with Linux ecosystem without leaving Windows aside. It might become very interesting start point for learning NET Core. I think the time has finally come.

And, I think the most important, we have learned something new. You never know when it comes helpful and how it will affect your work.