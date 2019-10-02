# UnVersion

[![Build status](https://img.shields.io/appveyor/ci/umco/umbraco-unversion.svg)](https://ci.appveyor.com/project/umco/umbraco-unversion)
[![NuGet release](https://img.shields.io/nuget/v/Our.Umbraco.UnVersion.svg)](https://www.nuget.org/packages/Our.Umbraco.UnVersion)
[![Our Umbraco project page](https://img.shields.io/badge/our-umbraco-orange.svg)](https://our.umbraco.org/projects/website-utilities/unversion/)


This package automaticaly removes any previous versions for those times when a version history isn't important, and you don't want to take up the database space.


## Getting Started

### Installation

> *Note:* UnVersion has been developed against **Umbraco v8.2.? (Pending PR)** and will support that version and above.

UnVersion can be installed from either Our Umbraco or NuGet package repositories, or build manually from the source-code:

#### Our Umbraco package repository

To install from Our Umbraco, please download the package from:

> <https://our.umbraco.org/projects/website-utilities/unversion/>

#### NuGet package repository

To [install from NuGet](https://www.nuget.org/packages/Our.Umbraco.UnVersion), you can run the following command from within Visual Studio:

	PM> Install-Package Our.Umbraco.UnVersion

We also have a [MyGet package repository](https://www.myget.org/gallery/umbraco-packages) - for bleeding-edge / development releases.

#### Manual build

If you prefer, you can compile UnVersion yourself, you'll need:

* Visual Studio 2017 (or above)

To clone it locally click the "Clone in Windows" button above or run the following git commands.

	git clone https://github.com/umco/umbraco-unversion.git umbraco-unversion
	cd umbraco-unversion
	.\build.cmd

---

## Developers Guide

For details on how to use the UnVersion package, please refer to our [Developers Guide](docs/developers-guide.md) documentation.

---

## Contributing to this project

Anyone and everyone is welcome to contribute. Please take a moment to review the [guidelines for contributing](CONTRIBUTING.md).

* [Bug reports](CONTRIBUTING.md#bugs)
* [Feature requests](CONTRIBUTING.md#features)
* [Pull requests](CONTRIBUTING.md#pull-requests)


## Contact

Have a question?

* [UnVersion Forum](https://our.umbraco.org/projects/website-utilities/unversion/bugs-feedback-and-suggestions/) on Our Umbraco
* [Raise an issue](https://github.com/umco/umbraco-unversion/issues) on GitHub


## Dev Team

* [Matt Brailsford](https://github.com/mattbrailsford)
* [Lee Kelleher](https://github.com/leekelleher)


## License

Copyright &copy; 2012 Matt Brailsford, Our Umbraco and [other contributors](https://github.com/umco/umbraco-unversion/graphs/contributors)

Licensed under the [MIT License](LICENSE.md)
