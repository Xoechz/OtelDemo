:::mermaid
flowchart TB
BE[BestellerService 1 bis n]
LA[LagerService 1 bis n]
AL[Anderes Lager]
LI[LieferantenService 1 bis n]
BE --Fragt zufälliges Lager nach Liste an Waren--> LA
LA --"50% pro Ware => Fragt zufälliges anderes Lager"--> AL
LA --"50% liefert Ware selbst, 10% Fehler, z.B. Nicht lieferbar, Mangel..."--> BE
LI --Beliefert zufälliges Lager mit Liste an Waren, 10% Fehler, z.B. Nicht lieferbar, Mangel...--> LA
LA --"50% pro Ware => Schickt es weiter an zufälliges anderes Lager"--> AL
LI --"50% behält sich Ware selbst, und meldet Annahme zurück"--- LA

linkStyle 0,1,2 stroke:#00ff00,stroke-width:1px
linkStyle 3,4,5 stroke:#0000ff,stroke-width:1px
:::
