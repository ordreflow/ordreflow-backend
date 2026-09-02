# Git Branching for the Backend

This is the quick branching guide for `ordreflow-backend`.

The complete project-wide strategy is maintained in the shared documentation repository:

[OrdreFlow Git Branching and Pull Requests](https://github.com/ordreflow/ordreflow-docs/blob/main/docs/branching-and-prs.md)

## Main Rules

- `main` is the stable branch.
- Do not push directly to `main`.
- Changes to `main` must go through a pull request.
- Pull requests to `main` require at least one approval from another team member.
- Successful build and test checks should be required when CI is available.
- Force pushes to `main` are not allowed.
- Delete branches after they have been merged.

## Branch Names

```text
feature/<issue-number>-<short-description>
fix/<issue-number>-<short-description>
docs/<issue-number>-<short-description>
chore/<issue-number>-<short-description>
```

Examples:

```text
feature/7-create-time-entry-endpoint
fix/19-invalid-duration-validation
docs/8-backend-readme
chore/2-flox-environment
```

## Independent Issues

For work that can be integrated safely on its own:

```text
main
  -> feature/<issue-number>-<description>
  -> pull request to main
```

## Parent and Child Issues

For a larger feature whose child issues together form one incomplete flow:

```text
main
  -> feature/m1-backend-poc
      -> feature/<issue-number>-domain-entity
      -> feature/<issue-number>-database-setup
      -> feature/<issue-number>-time-entry-endpoint
```

Child branches are created from the parent integration branch and their pull requests target that parent branch. When the complete feature has been integrated and tested, create a final pull request from the parent branch to `main`.

## Pull Requests

Every pull request should:

- Link to the relevant GitHub issue.
- Stay focused on one issue or child issue.
- Explain the changes.
- Include relevant testing information.
- Be reviewed by another team member.

Use regular merge for child pull requests into the parent branch. Use the agreed final merge method when merging the complete parent branch into `main`, as described in the shared strategy.

## Cross-Repository Work

If backend work is needed for frontend functionality:

- Keep the backend issue in this repository.
- Keep the frontend issue in `ordreflow-frontend`.
- Link the issues together.
- Use separate branches and pull requests.
- Merge in dependency order when necessary.

If this quick guide conflicts with the shared strategy, the shared strategy is authoritative.
