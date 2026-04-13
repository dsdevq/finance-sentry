import {expect, test} from '@playwright/test';

const STORIES = ['primary', 'secondary', 'destructive', 'loading', 'disabled', 'all-sizes'];

for (const story of STORIES) {
  test(`button/${story} — light`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-button--${story}&viewMode=story`);
    await page.waitForSelector('cmn-button, [data-testid]', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`button-${story}-light.png`);
  });

  test(`button/${story} — dark`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-button--${story}&viewMode=story`);
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-theme', 'dark');
    });
    await page.waitForSelector('cmn-button, [data-testid]', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`button-${story}-dark.png`);
  });
}
