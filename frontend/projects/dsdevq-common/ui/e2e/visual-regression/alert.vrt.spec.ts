import {expect, test} from '@playwright/test';

const STORIES = ['info', 'success', 'warning', 'error', 'with-title', 'dismissible', 'with-long-content'];

for (const story of STORIES) {
  test(`alert/${story} — light`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-alert--${story}&viewMode=story`);
    await page.waitForSelector('cmn-alert', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`alert-${story}-light.png`);
  });

  test(`alert/${story} — dark`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-alert--${story}&viewMode=story`);
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-theme', 'dark');
    });
    await page.waitForSelector('cmn-alert', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`alert-${story}-dark.png`);
  });
}
