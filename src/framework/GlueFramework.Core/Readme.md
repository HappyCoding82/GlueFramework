# Query by Lambda

### QueryAsync
```
Repository<NextNumbers> rep = ...;
rep.QueryAsync(x => x.Category != "");
```
### Query Top
```
Repository<NextNumbers> rep = ...;
rep.QueryTopAsync(x => x.Category != "",3);
```
### Query By FilterOption<Model>
```
Repository<NextNumbers> rep = ...;
rep.PagerSearchAsync(new FilterOptions<NextNumbers>(x => x.Category != "", new PagerInfo() { PageIndex =3, PageSize = 3 }));
```