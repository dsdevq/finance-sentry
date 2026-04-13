import {expect, test} from '@playwright/test';

const STORIES = ['default', 'elevated', 'no-padding', 'all-padding-sizes', 'with-nested-content'];

for (const story of STORIES) {
  test(`card/${story} — light`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-card--${story}&viewMode=story`);
    await page.waitForSelector('cmn-card', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`card-${story}-light.png`);
  });

  test(`card/${story} — dark`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-card--${story}&viewMode=story`);
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-theme', 'dark');
    });
    await page.waitForSelector('cmn-card', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`card-${story}-dark.png`);
  });
}
