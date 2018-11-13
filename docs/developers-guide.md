# UnVersion - Developers Guide

## Summary

This package automaticaly removes any previous versions for those times when a version history aren't important, and you don't want to take up the database space.
With Umbraco being such a versatile platform, it's very easy to develop small scale apps that go beyond the regular "Content" paradim. In some situations, version history becomes unimportant, and a waste, so this package automaticaly cleans up any previous versions when a new version is published.

## How to use

The package is configured via an unVersion.config file located in the default umbraco config folder.
To define a new unversion rule, create an `<add>` element with the following attributes.

```xml
<?xml version="1.0"?>
<unVersionConfig>
  <add docTypeAlias="newsPage" rootXpath="//node[@nodeTypeAlias='newsIndex']" maxDays="2" maxCount="10" />
</unVersionConfig>
```

**`docTypeAlias`** _[Optional]_ - The doc type alias of the documents to be unversioned, if undefined, applies to all (If you don't define `docTypeAlias`, it is advised you define rootXpath to narrow down the scope of the unversion).

**`rootXpath`** _[Optional]_ - An XPath statement to define the root folder for the unversion rule, e.g. `"//NewsIndex"`

**`maxDays`** _[Optional]_ - The maximum number of days to keep versions. If attribute is not present, defaults to `int.MaxValue`.

**`maxCount`** _[Optional]_ - The maximum number of latest versions to keep. If attribute is not present, defaults to `int.MaxValue`.


---

### Useful Links

* [Source Code](https://github.com/umco/umbraco-unversion)
* [Our Umbraco Project Page](https://our.umbraco.org/projects/website-utilities/unversion/)
