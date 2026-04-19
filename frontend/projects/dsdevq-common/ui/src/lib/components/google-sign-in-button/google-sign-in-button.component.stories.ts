import type {Meta, StoryObj} from '@storybook/angular';

import {GoogleSignInButtonComponent} from './google-sign-in-button.component';

const meta: Meta<GoogleSignInButtonComponent> = {
  title: 'Components/GoogleSignInButton',
  component: GoogleSignInButtonComponent,
  tags: ['autodocs'],
  parameters: {
    docs: {
      description: {
        component:
          'Renders the Google Identity Services (GSI) sign-in button. Requires a valid `clientId` from Google Cloud Console. In Storybook the button renders but the GSI popup will not complete without a real OAuth origin.',
      },
    },
  },
};

export default meta;
type Story = StoryObj<GoogleSignInButtonComponent>;

export const Default: Story = {
  render: args => ({
    props: args,
    template: '<cmn-google-sign-in-button [clientId]="clientId" />',
  }),
  args: {
    clientId: 'your-google-client-id.apps.googleusercontent.com',
  },
};

export const CustomWidth: Story = {
  render: args => ({
    props: args,
    template: '<cmn-google-sign-in-button [clientId]="clientId" [buttonConfiguration]="buttonConfiguration" />',
  }),
  args: {
    clientId: 'your-google-client-id.apps.googleusercontent.com',
    buttonConfiguration: {
      type: 'standard',
      shape: 'rectangular',
      theme: 'outline',
      text: 'signin_with',
      size: 'large',
      width: 240,
    },
  },
};
