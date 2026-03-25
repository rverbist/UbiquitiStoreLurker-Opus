I want a system that monitors a list of web storefront product pages for changes in availability.

The system should poll a set of urls at a random interval within a certain range, rate limited by a single queue, resilient (retry, exp. backoff) and respectful (429 etc). Get each url, inspect the html, run several tests to determine wheter the item is in stock or not. I will list some but this list is not exhaustive. I will provide two url - one in stock - one out of stock - use browser emulation to analyze both page's html structure but also the additional web requests they initiate - maybe there's an internal API that contains the stock state - use browser emulation for discovery only, the final request method should be plain old HTTP. While you're there, learn how to grab the product info (name, code, description, images).

Example - Sold Out - <https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-4>
```html
<div> Sold Out</div>
<span>Sold Out</div>
<div><label>...</label>Notify me via email when available.</div>
<button variant="unavailable" label="Sold Out" disabled="">...</button>
```

Example - Available - <https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-8>
<span>Add to Cart</span>
<input type="number" inputmode="numeric" min="1" value="1">

I was logged into the store at the time of writing these examples, and this is just a snapshot -- on 23/03/2026 04:38 the unas-pro-4 was sold out and the unas-pro-8 was in stock. That won't always be the case, when in doubt ask me to confirm stock state before drawing any conclusions from the data.

The system is to send only one notification per state change per url (product), with the exception of the initial state (discovered on first request). Notifications are sent via various configurable providers: a browser notification, an email, an SMS, a message on Teams, Discord, WhatsApp, Facebook and Discord.

Read and persist the product information as part of the initial request / bootstrap. If the product information happens to be on a resouce you had to request anyway to get the stock state, take that opportunity to extract, compare and update if needed. The product code is immutable, shit the bed if that changes.

The user interacts with the system through a web portal, the following endpoints are available
/ - redirect to /monitor
/setup - configuration form
/monitor - overview page showing (1) each url (product) and the stock state (2) requests made in the last hour (max 100, recent first) (3) request log is updated in real time as requests happen, the oldest requests are removed as new ones come in.
/api/health - standard healthchecks api
/api/metrics - prometheus scrape endpoint

The application is a .NET 10 ASP.NET minimal web application intended to run on docker as primary target - setup environment as such.
Use Entity Framework Core with a SQLite database (same container) - all configuration (other than db and log) and requests go in the db.
Use Serilog for logging - use CLEF. Sane setup.
The application is single user without authentication.
The application will be behind a reverse proxy so only HTTP is required.
Use SignalR for the real-time request feed.
Yes, add unit tests (nunit4) but don't go full retard on the coverage

As for front-end framework... I have no horse in this race. Pick a community approved active-development well-documented battle-tested lightweight framework, this is not a complex application. As for theming, stay in the same theme and style as the webstore being polled - Ubiquiti - keep it "light". Make a nice visual of the products and their stock state - I'm thinking a grid of thumbnails of the products, nice label, border color signals stock state, bell icon if "subscribed" to the next stock state change. When clicked, make it stand out from the rest and open a detail panel to the right. Detail panel shows full product info, images, request telemetry (count, last one, next one, errors, retries) and stock telemetry (current state, date of last change, number of days since). Below that, polling event history ledger - a table showing the http method, url, product details, stock state, error info (if any) sorted recent first. Updated in real time, rows are inserted at the top - add a nice animation to the whole insertion event. Make the bottom row tumble off the table and fall down off screen.

When a subscribed to "in stock" change happens the website will show a giant pop-up with the product info, image, big ass text saying it's stock and to click here - the entire modal is a link to the product page on the store. The "enter" animation of the pop-up must contain PowerPoint 98 levels of energy. As long as the pop-up is on screen, confetti falls down [the pop-up is in front of the confetti, the confetti is infront of everything else]

Configuration page - KISS
user (nick)name, email (opt), phone (opt)
global rate limit and retry settings
notification provider configuration - user can select any provider type (browser, email, sms, Teams, WhatsApp, Facebook, Discord) any number of times, each configuration can be enabled/disabled (enable only available when fully configured) - standard seed is one of each with only browser enabled.
product configuration - user provides an url (must be unique), can enable/disable, can select subscribed events (none, in, out, both).

Goal is two-fold - learn new tech and compensate for the incompetence of others
Research topics (new):

- containerized web application + db (docker)
- sqllite
- agent-based web discovery via headless browser
- exotic messaging API's: Teams, WhatsApp, Facebook and Discord
- web-dev stuff because that shit moves fast - prometheus/grafana/loki/signalr/$frontendframework/animations/notifications FUCK IT add Aspire as well

Write a comprehensive plan to build this application in a controlled and structured way. Do not write any code yet. You are only allowed to perform preliminary research (online sources, library/framework choices, specific nuget packages, API documentation...) to be used further down the line in the implementation or as needed to build the plan. *No* discovery of the webstore pages at this point.

Include additional documentation and guidance on the research topics - pull in interesting features not listed if there are any.

**Important** Include my entire prompt, as-is, unmodified, everything above this line, in a seperate markdown file named prompt.md in the plan directory.
I'm done, you're up.
