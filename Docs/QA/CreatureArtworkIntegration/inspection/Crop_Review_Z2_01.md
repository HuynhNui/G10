# Z2_Creature_01 adopted crop review

The initial runtime export retained the complete native group with nonzero-alpha
bounds `[75,184,749,1047]` and 24px padding (722 × 911). After viewing that
actual initial Photo Lab prototype and baked image, the integration owner approved
the explicit source region below. The runtime PNG now measures **654 × 615** with
24px transparent padding. The original PSD and complete inspection PNG remain intact.

The large empty gap below the organism is caused by a visibly disconnected short
blue stroke at source `[324,1034,373,1047]`, containing 146 nonzero-alpha pixels.
It is painted into both `[0,2,7]` **nét** and `[0,2,9] **Layer 59**, which also
contain necessary organism linework. These whole layers cannot be excluded.

The disconnected small blue/gray dot on the right is at source
`[740,345,749,354]`, 75 nonzero-alpha pixels. It is part of `[0,2,4]` **Layer 54**,
which also contains organism linework. Do not exclude that layer either.

The adopted explicitly reviewed source region `[0,0,720,800]` omits those two disconnected
marks while retaining every body, fin, tail, colored halo and white drip. All
nonzero alpha retained inside this region has exact bounds `[75,184,681,751]`.
The longest white drip ends at y751; the proposed region leaves another 49px
before its lower boundary. After removing only transparent outer padding and
adding the standard 24px padding, the output is **654 × 615**,
aspect **1.0634146341**.

This is an explicit bounded crop based on inspecting native pixels and individual
layer renders, not automatic anatomical extraction or erasing within the organism.
`creature_mapping.json` records the exact region and approval basis; the exporter
reproduces it. Exactly **221** nonzero-alpha pixels lie outside the accepted region.
All content pixels inside the region are byte-identical to the native PSD group.

Evidence under the shared PSD's `detail/` directory:

- `Z2_01-on-gray.png`: complete native group on a contrasting gray background.
- `Z2_01-children-contact.png` and `Z2_01-children.json`: all eleven child layers.
- `Z2_01-child-07.png`, `Z2_01-child-09.png`: linework plus bottom mark.
- `Z2_01-child-04.png`: linework plus right dot.
- `Z2_01-proposed-crop-review.png`: the reviewed bounded region on gray, retained
  as inspection evidence. Runtime receives the native transparent region plus
  padding, never this gray inspection background.
