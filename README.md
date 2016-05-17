Let you specify one or more document types that will be excluded from Umbraco-generated URLs, thus making them "invisible". Those can be used as grouping nodes and they will not appear as part of the URL.

##Usage
After you install the package, you will have to add one appSettings entry to your web.config file, as follows:

```xml
<add key="virtualnode" value="docTypeToMakeVirtual"/>
```
Where docTypeToMakeVirtual is the document type alias you want to be treated as a "virtual" node.

You can define more than one "rules" for docTypes by separating them with commas, as follows:

```xml
<add key="virtualnode" value="docTypeToMakeVirtual,anotherDocType"/>
```

You can also use wildcards at the start and/or the end of the document type alias, like this:

```xml
<add key="virtualnode" value="dog*,*cat,*mouse*"/>
```
This means that all document type aliases ending with "dog", all document types starting with "cat" and all those containing "mouse" will be treated as virtual nodes. 

##Auto numbering of nodes

Consider the following example:

articles
  groupingNode1
    article1
    article2
  groupingNode2
    
Supposing that groupingNode1 and groupingNode2 are virtual nodes, the path for article1 will be /articles/article1. Okay, but what if we add a new article named "article1" under groupingNode2?

The plugin checks nodes on publish and changes their names 
