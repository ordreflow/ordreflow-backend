# Backend unit-test coverage — 2026-11-04

The [Cobertura XML report](coverage.cobertura.xml) was generated from the existing
`Backend/Feature_Test` project using .NET SDK `8.0.406` and
`coverlet.collector` `6.0.0`. The filtered run passed **303 tests**, with no
failures or skips. The XML records collection at **2026-11-04 08:40:24 UTC**.

| Production area | Covered / valid lines | Line coverage |
| --- | ---: | ---: |
| Application handlers (subset of Application) | 704 / 1,026 | 68.6% |
| Application assembly | 869 / 1,303 | 66.7% |
| Domain | 573 / 748 | 76.6% |
| OperationResultPattern | 37 / 43 | 86.0% |
| ObjectMapper | 0 / 25 | 0.0% |
| WebApi | 72 / 888 | 8.1% |
| Persistence | 0 / 2,679 | 0.0% |
| **All instrumented production assemblies** | **1,551 / 5,686** | **27.3%** |

The assembly figures come from the Cobertura package summaries. The handler
figure combines distinct source lines in the 36 `Application/Handlers/` files,
counting each filename and line number once (including async-generated classes
attributed to those files). **Do not add the handler row to the Application row**:
it is already included there.

## Reproduce

From the repository root on an x86_64 Linux checkout where the committed Flox
environment is already materialized and the project's NuGet restore assets
already exist:

```sh
SDK=.flox/run/x86_64-linux.ordreflow-backend-dev/bin/dotnet
"$SDK" --version  # 8.0.406
"$SDK" test Backend/Feature_Test/Feature_Test.csproj --no-restore \
  --collect:"XPlat Code Coverage" \
  --filter 'FullyQualifiedName!~Auth.AuthorizationTests&FullyQualifiedName!~HttpTests' \
  --results-directory /tmp/ordreflow-unit-coverage
```

For a fresh checkout, provision the committed Flox environment and restore the
test project with SDK `8.0.406` first; `--no-restore` deliberately does not do
that work. Restoring may require network access. **Flox activation initializes
temporary PostgreSQL data even with services disabled**, so the command above
uses its already-materialized SDK directly and does not run the activation hook.

The collector writes `coverage.cobertura.xml` beneath a run-ID subdirectory of
the results directory. Open that XML with a Cobertura-compatible coverage viewer
or CI tool; no additional .NET package is needed to generate it. This saved
copy is the **raw collector output** and embeds the generating machine's absolute
`Backend/` source path; regenerate it locally or configure source-path mapping if
your viewer cannot find the source files on another machine.

## Scope and limitations

This is coverage from **non-HTTP Feature_Test tests only**. The filter excludes
`AuthorizationTests` and `*HttpTests`, and does not run the separate `WebApiTests`
project or PostgreSQL tests. In particular, 0% Persistence coverage in this run
does **not** mean its behavior was tested and found faulty. Faked repositories
cannot verify database scoping or aggregation; line coverage is not a measure
of requirement acceptance or branch coverage.

The run completed with existing warnings: preview DependencyInjection 11
packages have not been tested with .NET 8, there is an `MSB3277` dependency
version conflict, and existing compiler warnings remain.
