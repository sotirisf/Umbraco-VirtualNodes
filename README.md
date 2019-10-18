Lets you specify one or more document types that will be excluded from Umbraco-generated URLs, thus making them "invisible". Those can be used as grouping nodes and they will not appear as part of the URL.

##Usage
After you install the package, you will have to add one appSettings entry to your web.config file, as follows:

```xml
<add key="virtualnode" value="docTypeToMakeVirtual"/>
<add key="virtualnode-notpage" value="docTypeThatDoNotHaveTemplate"/>
```
Where docTypeToMakeVirtual is the document type alias you want to be treated as a "virtual" node and docTypeThatDoNotHaveTemplate is the document type alias that do not have any template or it's not a public page.

The docTypeThatDoNotHaveTemplate setting is important due to a perfomance issue [#11](https://github.com/sotirisf/Umbraco-VirtualNodes/issues/11), so please remember to set it.

You can define more than one "rules" for docTypes by separating them with commas, as follows:

```xml
<add key="virtualnode" value="docTypeToMakeVirtual,anotherDocType"/>
<add key="virtualnode-notpage" value="docTypeThatDoNotHaveTemplate,anotherDocType"/>
```

You can also use wildcards at the start and/or the end of the document type alias, like this:

```xml
<add key="virtualnode" value="dog*,*cat,*mouse*"/>
<add key="virtualnode-notpage" value="dog*,*cat,*mouse*"/>
```
This means that all document type aliases ending with "dog", all document types starting with "cat" and all those containing "mouse" will be treated as virtual nodes. 

##Advanced: Auto numbering of nodes

Consider the following example:

```
articles
  groupingNode1
    article1
    article2
  groupingNode2
```   
 
Supposing that groupingNode1 and groupingNode2 are virtual nodes, the path for article1 will be /articles/article1. Okay, but what if we add a new article named "article1" under groupingNode2?

The plugin checks nodes on save and changes their names accordingly to protect you from this. More specifically, it checks if the parent node of the node being saved is a virtual node and, if so, it checks all its sibling nodes to see if there is already another node using the same name. It then adjusts numbering accordingly.

So, if you saved a new node named "article1" under "groupingNode2" it would become:

```
articles
  groupingNode1
    article1
    article2
  groupingNode2
    article1 (1)
```

And then if you saved another node named "article1" again under "groupingNode1" it would become "article1 (2)" like this:

```
articles
  groupingNode1
    article1
    article2
    article1 (2)
  groupingNode2
    article1 (1)
```

This ensures that there are no duplicate urls. Of course, to keep things simple, I've implemented checks only a level up - if you have multiple virtual nodes under each other and multiple nodes with the same name in different levels, better be careful because no checks there. (Yes, I'm lazy).

