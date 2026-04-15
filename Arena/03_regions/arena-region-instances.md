# Arena Region Instances

## Pattern
A concrete arena region instance should point to reusable arena rule logic rather than owning a unique ruleset by default.

## First instance
The Britain graveyard test region is the first arena region instance used to validate the phase 1 enforcement shell.

## Expansion path
Later arena region instances should be able to reuse the same arena rules profile and controller structure with minimal translation.